using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;
using StackExchange.Redis;

namespace PvvBff.Infrastructure.Ingress;

/// <summary>
/// Reads a company's config blob Redis-first (key <c>pvv:config:{hash}:{type}</c>,
/// kept warm by pvv-config's CacheSyncWorker) and falls back to pvv-config's
/// internal by-hash HTTP endpoint when the cache misses.
/// </summary>
public sealed class RedisCompanyConfigReader : ICompanyConfigReader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InternalServicesOptions _options;
    private readonly ILogger<RedisCompanyConfigReader> _logger;

    public RedisCompanyConfigReader(
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        IOptions<InternalServicesOptions> options,
        ILogger<RedisCompanyConfigReader> logger)
    {
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<object?> GetByHashAsync(string hashedCompanyId, string configurationType, CancellationToken ct)
    {
        // 1) Redis-first.
        var key = $"pvv:config:{hashedCompanyId}:{configurationType}";
        var cached = await _redis.GetDatabase().StringGetAsync(key);
        if (!cached.IsNullOrEmpty)
            return Parse(cached!);

        // 2) Fallback to pvv-config's internal by-hash endpoint.
        _logger.LogInformation("Config cache miss for {Key}; falling back to pvv-config", key);
        var url = $"{_options.ConfigBaseUrl}/api/configurations/internal/{hashedCompanyId}/{configurationType}";
        try
        {
            using var response = await _httpClientFactory.CreateClient("internal").GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            return Parse(body);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Config fallback to pvv-config failed for {Key}", key);
            return null;
        }
    }

    private static object? Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return raw;
        }
    }
}
