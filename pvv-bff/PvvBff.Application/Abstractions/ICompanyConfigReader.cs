namespace PvvBff.Application.Abstractions;

/// <summary>
/// Reads a company's configuration blob, Redis-first (the cache pvv-config keeps
/// warm via its CacheSyncWorker) and falling back to pvv-config's by-hash HTTP
/// endpoint on a cache miss.
/// </summary>
public interface ICompanyConfigReader
{
    Task<object?> GetByHashAsync(string hashedCompanyId, string configurationType, CancellationToken ct);
}
