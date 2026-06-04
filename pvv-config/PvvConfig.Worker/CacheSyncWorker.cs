namespace PvvConfig.Worker;

/// <summary>
/// Background service that keeps Redis in sync with the configuration stored in
/// SQL Server. Skeleton only — the sync logic lands in Sprint 1.
/// </summary>
public class CacheSyncWorker(ILogger<CacheSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CacheSyncWorker started");

        // TODO Sprint 1: subscribe to the "pvv:cache:invalidate" Redis Pub/Sub
        //                channel, run an initial SQL -> Redis sync, and loop with a
        //                PeriodicTimer every 5 minutes as a fallback.

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
