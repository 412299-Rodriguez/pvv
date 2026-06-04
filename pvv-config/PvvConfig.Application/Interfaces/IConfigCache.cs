namespace PvvConfig.Application.Interfaces;

/// <summary>Redis-backed cache for company configurations.</summary>
public interface IConfigCache
{
    Task<string?> GetAsync(string key, CancellationToken ct);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);

    /// <summary>Publishes a cache-invalidation message on the "pvv:cache:invalidate" channel.</summary>
    Task PublishInvalidateAsync(string hashedCompanyId, string configurationType, CancellationToken ct);

    /// <summary>Builds the Redis key for a company configuration.</summary>
    static string BuildKey(string hashedCompanyId, string configurationType) =>
        $"pvv:config:{hashedCompanyId}:{configurationType}";
}
