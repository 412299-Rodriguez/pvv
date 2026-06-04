using System.Text.Json;
using PvvConfig.Application.Interfaces;
using StackExchange.Redis;

namespace PvvConfig.Infrastructure.Services;

public class RedisConfigCache(IConnectionMultiplexer redis) : IConfigCache
{
    public const string InvalidationChannel = "pvv:cache:invalidate";

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct) =>
        _db.StringSetAsync(key, value, ttl);

    public Task RemoveAsync(string key, CancellationToken ct) =>
        _db.KeyDeleteAsync(key);

    public Task PublishInvalidateAsync(string hashedCompanyId, string configurationType, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            hashedCompanyId,
            configurationType
        });
        return redis.GetSubscriber().PublishAsync(RedisChannel.Literal(InvalidationChannel), payload);
    }
}
