using System.Text.Json;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;
using PvvBff.Domain.Ingress;
using StackExchange.Redis;

namespace PvvBff.Infrastructure.Ingress;

/// <summary>Reads ingress routes from Redis under the key <c>route:{hash}</c>.</summary>
public sealed class RedisRouteStore : IRouteStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRouteStore> _logger;

    public RedisRouteStore(IConnectionMultiplexer redis, ILogger<RedisRouteStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RouteConfig?> FindAsync(string hash, CancellationToken ct)
    {
        var value = await _redis.GetDatabase().StringGetAsync($"route:{hash}");
        if (value.IsNullOrEmpty)
            return null;

        try
        {
            // Explicit ToString(): a RedisValue is ambiguous between the string and
            // ReadOnlySpan<byte> Deserialize overloads.
            return JsonSerializer.Deserialize<RouteConfig>(value.ToString(), JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ingress: corrupt route config in Redis for hash {Hash}", hash);
            return null;
        }
    }
}
