using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads;
using PvvBff.Infrastructure.Leads;

namespace PvvBff.Infrastructure.Payments;

/// <summary>
/// Periodic abandonment sweep, in two passes.
///
/// First the payment one: transactions left Pending past the TTL become
/// Abandoned (the visitor opened the checkout and never came back). Then the
/// funnel one: any lead still active with no recent activity is closed as
/// abandoned, which is what catches the visitors who left at the plate, the
/// holder or the quote — long before any payment existed.
///
/// The order matters: payment abandonment records the richer step-4 detail, and
/// the generic pass then skips those leads because they are no longer active.
/// </summary>
public sealed class AbandonmentDetectionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PaymentOptions _options;
    private readonly LeadOptions _leadOptions;
    private readonly ILogger<AbandonmentDetectionJob> _logger;

    public AbandonmentDetectionJob(
        IServiceScopeFactory scopeFactory,
        IOptions<PaymentOptions> options,
        IOptions<LeadOptions> leadOptions,
        ILogger<AbandonmentDetectionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _leadOptions = leadOptions.Value;
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
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
            var leads = scope.ServiceProvider.GetRequiredService<ILeadStore>();
            var projection = scope.ServiceProvider.GetRequiredService<ILeadProjectionService>();

            // Pass 1 — checkouts that were opened and never completed.
            var paymentThreshold = DateTime.UtcNow.AddMinutes(-_options.AbandonmentTtlMinutes);
            var stale = await repository.MarkAbandonedAsync(paymentThreshold, ct);
            foreach (var transaction in stale)
            {
                if (string.IsNullOrWhiteSpace(transaction.FlowId))
                    continue;

                await projection.ProjectAsync(
                    new LeadEvent(
                        LeadEventNames.PaymentAbandoned,
                        transaction.FlowId,
                        transaction.CompanyToken,
                        transaction.SessionId,
                        Payload: null,
                        DateTime.UtcNow),
                    ct);
            }

            if (stale.Count > 0)
                _logger.LogInformation("Marked {Count} stale pending payment(s) as abandoned", stale.Count);

            // Pass 2 — visitors who left before ever reaching the checkout.
            var leadThreshold = DateTime.UtcNow.AddMinutes(-_leadOptions.AbandonmentTtlMinutes);
            var abandonedLeads = await leads.MarkStaleAsAbandonedAsync(leadThreshold, ct);
            if (abandonedLeads > 0)
                _logger.LogInformation("Marked {Count} inactive lead(s) as abandoned", abandonedLeads);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Abandonment scan failed");
        }
    }
}
