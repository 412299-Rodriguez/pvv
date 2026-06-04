using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PvvSoat.Domain.Enums;
using PvvSoat.Infrastructure.Persistence;

namespace PvvSoat.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically expires budgets whose validity window has passed.
/// Runs every 5 minutes; a failed cycle is logged and the service keeps running.
/// </summary>
public class BudgetExpirationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetExpirationJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BudgetExpirationJob started");

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ExpireBudgetsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BudgetExpirationJob cycle failed; will retry next tick");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireBudgetsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SoatDbContext>();

        var now = DateTime.UtcNow;
        var expiredCount = await context.Budgets
            .Where(b => b.Status == BudgetStatus.Active && b.ValidUntil < now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.Status, BudgetStatus.Expired), ct);

        if (expiredCount > 0)
        {
            logger.LogInformation("BudgetExpirationJob expired {Count} budget(s)", expiredCount);
        }
    }
}
