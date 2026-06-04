using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PvvConfig.Infrastructure.Persistence;
using StackExchange.Redis;

namespace PvvConfig.Worker;

/// <summary>
/// Keeps Redis in sync with the configuration stored in SQL Server. Runs an
/// initial full sync, subscribes to the "pvv:cache:invalidate" Pub/Sub channel
/// for targeted updates, and runs a full sync every 5 minutes as a fallback.
/// </summary>
public class CacheSyncWorker(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<CacheSyncWorker> logger) : BackgroundService
{
    private const string InvalidationChannel = "pvv:cache:invalidate";
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CacheSyncWorker started - running initial sync");

        try
        {
            await SyncAllCompaniesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Initial cache sync failed");
        }

        await SubscribeToInvalidationsAsync(stoppingToken);

        using var timer = new PeriodicTimer(SyncInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SyncAllCompaniesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Periodic cache sync failed; will retry next tick");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested.
        }
    }

    private async Task SubscribeToInvalidationsAsync(CancellationToken ct)
    {
        try
        {
            var subscriber = redis.GetSubscriber();
            var queue = await subscriber.SubscribeAsync(RedisChannel.Literal(InvalidationChannel));
            queue.OnMessage(channelMessage => HandleInvalidationAsync(channelMessage.Message, ct));
            logger.LogInformation("Subscribed to Redis channel '{Channel}'", InvalidationChannel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to subscribe to the Redis invalidation channel");
        }
    }

    private async Task HandleInvalidationAsync(RedisValue message, CancellationToken ct)
    {
        try
        {
            if (message.IsNullOrEmpty)
            {
                return;
            }

            var payload = JsonSerializer.Deserialize<InvalidationMessage>(message.ToString(), JsonOptions);
            if (payload is null || string.IsNullOrEmpty(payload.HashedCompanyId))
            {
                return;
            }

            await SyncCompanyConfigAsync(payload.HashedCompanyId, payload.ConfigurationType, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process cache invalidation message");
        }
    }

    private async Task SyncAllCompaniesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
        var db = redis.GetDatabase();

        var companies = await context.Companies.AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        var syncedKeys = 0;
        foreach (var company in companies)
        {
            var configs = await context.Configurations.AsNoTracking()
                .Where(c => c.CompanyId == company.CompanyId && c.IsActive)
                .ToListAsync(ct);

            foreach (var config in configs)
            {
                var key = BuildKey(company.HashedCompanyId, config.ConfigurationType);
                await db.StringSetAsync(key, config.ConfigurationValue, CacheTtl);
                syncedKeys++;
            }
        }

        logger.LogInformation(
            "Cache sync complete: {Keys} configuration(s) across {Companies} company(ies)",
            syncedKeys, companies.Count);
    }

    private async Task SyncCompanyConfigAsync(string hashedCompanyId, string configurationType, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
        var db = redis.GetDatabase();
        var key = BuildKey(hashedCompanyId, configurationType);

        var company = await context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.HashedCompanyId == hashedCompanyId, ct);

        if (company is null)
        {
            await db.KeyDeleteAsync(key);
            logger.LogWarning("No company for hash; removed stale key '{Key}'", key);
            return;
        }

        var config = await context.Configurations.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.CompanyId == company.CompanyId
                     && c.ConfigurationType == configurationType
                     && c.IsActive, ct);

        if (config is null)
        {
            await db.KeyDeleteAsync(key);
            logger.LogInformation("No active configuration; removed key '{Key}'", key);
        }
        else
        {
            await db.StringSetAsync(key, config.ConfigurationValue, CacheTtl);
            logger.LogInformation("Synced key '{Key}'", key);
        }
    }

    private static string BuildKey(string hashedCompanyId, string configurationType) =>
        $"pvv:config:{hashedCompanyId}:{configurationType}";

    private sealed record InvalidationMessage(string HashedCompanyId, string ConfigurationType);
}
