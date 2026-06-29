using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Payments;

/// <summary>
/// Periodically marks payment transactions that have stayed Pending past the TTL
/// as Abandoned (the user started checkout but never completed payment). Feeds the
/// abandoned-cart recovery the admin dashboard surfaces later.
/// </summary>
public sealed class AbandonmentDetectionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PaymentOptions _options;
    private readonly ILogger<AbandonmentDetectionJob> _logger;

    public AbandonmentDetectionJob(
        IServiceScopeFactory scopeFactory,
        IOptions<PaymentOptions> options,
        ILogger<AbandonmentDetectionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.AbandonmentScanSeconds <= 0 ? 300 : _options.AbandonmentScanSeconds);
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await ScanAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        try
        {
            var threshold = DateTime.UtcNow.AddMinutes(-_options.AbandonmentTtlMinutes);

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

            var count = await repository.MarkAbandonedAsync(threshold, ct);
            if (count > 0)
                _logger.LogInformation("Marked {Count} stale pending payment(s) as abandoned", count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Abandonment scan failed");
        }
    }
}
