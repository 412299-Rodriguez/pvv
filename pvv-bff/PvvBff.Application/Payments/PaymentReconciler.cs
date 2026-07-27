using MediatR;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Payments;

/// <summary>
/// Asks the provider about transactions we still consider unpaid, and applies whatever
/// it answers.
///
/// This exists because a notification is not a guarantee. It can be lost, it cannot
/// reach a developer's machine at all, and for a cash coupon the payment happens at a
/// kiosk days after the browser is gone. Without someone asking, those sales are
/// invisible: money taken and no policy issued.
/// </summary>
public interface IPaymentReconciler
{
    /// <summary>Returns how many transactions this pass turned into confirmed sales.</summary>
    Task<int> ReconcileAsync(DateTime createdAfter, CancellationToken ct);
}

public sealed class PaymentReconciler : IPaymentReconciler
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentGateway _gateway;
    private readonly ISender _mediator;
    private readonly ILogger<PaymentReconciler> _logger;

    public PaymentReconciler(
        IPaymentRepository repository,
        IPaymentGateway gateway,
        ISender mediator,
        ILogger<PaymentReconciler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<int> ReconcileAsync(DateTime createdAfter, CancellationToken ct)
    {
        var candidates = await _repository.GetReconcilableAsync(createdAfter, ct);
        if (candidates.Count == 0)
            return 0;

        var confirmed = 0;
        foreach (var transaction in candidates)
        {
            GatewayPayment? payment;
            try
            {
                payment = await _gateway.FindPaymentByTransactionAsync(transaction.Id, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One unreachable lookup must not abort the pass: the transactions
                // after this one are exactly the ones at risk of being abandoned.
                _logger.LogWarning(ex, "Reconciliation lookup failed for {TransactionId}", transaction.Id);
                continue;
            }

            // Null covers both "the provider has nothing for this yet" and the mock
            // gateway, which has no provider to ask — so this is a no-op there.
            if (payment is null)
                continue;

            var result = await _mediator.Send(
                new ConfirmPaymentCommand(
                    transaction.Id, payment.Outcome, payment.ProviderPaymentId, payment.ExpiresAt),
                ct);

            if (result.Published)
            {
                confirmed++;
                _logger.LogInformation(
                    "Reconciliation confirmed {TransactionId} ({RawStatus}) — its notification never arrived",
                    transaction.Id, payment.RawStatus);
            }
        }

        return confirmed;
    }
}
