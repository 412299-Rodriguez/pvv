using MediatR;
using Microsoft.Extensions.Logging;
using PvvConfig.Application.Interfaces;

namespace PvvConfig.Application.Configurations.Queries;

/// <summary>
/// Read path for pvv-bff: Redis-first, then SQL fallback (decrypting the hashed
/// company id), repopulating Redis on a miss. Returns the raw JSON value.
/// </summary>
public record GetConfigurationByHashQuery(string HashedCompanyId, string ConfigurationType)
    : IRequest<string?>;

public class GetConfigurationByHashHandler(
    IConfigCache cache,
    IEncryptionService encryption,
    IConfigurationRepository configurations,
    ILogger<GetConfigurationByHashHandler> logger)
    : IRequestHandler<GetConfigurationByHashQuery, string?>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    public async Task<string?> Handle(GetConfigurationByHashQuery request, CancellationToken ct)
    {
        var key = IConfigCache.BuildKey(request.HashedCompanyId, request.ConfigurationType);

        // Fast path: Redis.
        var cached = await cache.GetAsync(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        // Slow path: decrypt the hash, read SQL, repopulate Redis.
        Guid companyId;
        try
        {
            companyId = Guid.Parse(encryption.Decrypt(request.HashedCompanyId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decrypt hashed company id");
            return null;
        }

        var configuration = await configurations.GetActiveAsync(companyId, request.ConfigurationType, ct);
        if (configuration is null)
        {
            return null;
        }

        await cache.SetAsync(key, configuration.ConfigurationValue, CacheTtl, ct);
        return configuration.ConfigurationValue;
    }
}
