using MediatR;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads;
using PvvBff.Domain.Payments;

namespace PvvBff.Application.Payments;

/// <summary>Apply what the provider says about a payment to our transaction.</summary>
/// <param name="ProviderPaymentId">
/// The provider's payment id, when there is a payment to point at. Absent from the
/// mock gateway, which reports an outcome and nothing else.
/// </param>
/// <param name="PendingUntil">Deadline of a still-unpaid payment (a cash coupon).</param>
public sealed record ConfirmPaymentCommand(
    string TransactionId,
    PaymentOutcome Outcome,
    string? ProviderPaymentId = null,
    DateTime? PendingUntil = null) : IRequest<ConfirmPaymentResult>;

public sealed record ConfirmPaymentResult(bool Found, bool Published, string Status);

/// <summary>
/// Confirms (or fails) a payment. On the first approval it marks the transaction
/// Confirmed and publishes an EmissionMessage to RabbitMQ.
///
/// Reached from three places that can all fire for the same payment: the provider's
/// webhook, the result page's reconciliation, and the background reconciliation in
/// the abandonment sweep. Being idempotent is therefore not a nicety — it is the
/// only thing standing between a buyer and two policies.
/// </summary>
public sealed class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, ConfirmPaymentResult>
{
    private readonly IPaymentRepository _repository;
    private readonly IEmissionPublisher _publisher;
    private readonly ILeadProjectionService _leads;
    private readonly ILogger<ConfirmPaymentHandler> _logger;

    public ConfirmPaymentHandler(
        IPaymentRepository repository,
        IEmissionPublisher publisher,
        ILeadProjectionService leads,
        ILogger<ConfirmPaymentHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _leads = leads;
        _logger = logger;
    }

    public async Task<ConfirmPaymentResult> Handle(ConfirmPaymentCommand request, CancellationToken ct)
    {
        var transaction = await _repository.GetAsync(request.TransactionId, ct);
        if (transaction is null)
        {
            _logger.LogWarning("Payment notification for unknown transaction {TransactionId}", request.TransactionId);
            return new ConfirmPaymentResult(Found: false, Published: false, Status: "not_found");
        }

        // Cheap fast path; the authoritative guard is the compare-and-set below.
        if (transaction.Status == PaymentStatus.Confirmed)
            return new ConfirmPaymentResult(Found: true, Published: false, Status: "already_confirmed");

        // Still in flight — decide nothing about the sale. This is where the pre-HU-11
        // code was wrong: it treated anything that was not "approved" as a failure, and
        // Mercado Pago's pending/in_process (cash coupon, transfer, fraud review) would
        // have killed sales that were about to succeed.
        //
        // But "decide nothing" is not "record nothing". When a payment exists and is
        // merely unpaid, its id and deadline are exactly what stops the abandonment
        // sweep from discarding the sale half an hour into a three-week coupon.
        if (request.Outcome == PaymentOutcome.Pending)
        {
            if (!string.IsNullOrWhiteSpace(request.ProviderPaymentId))
            {
                await _repository.MarkPendingPaymentAsync(
                    transaction.Id, request.ProviderPaymentId, request.PendingUntil, ct);
            }

            return new ConfirmPaymentResult(Found: true, Published: false, Status: "pending");
        }

        if (request.Outcome == PaymentOutcome.Rejected)
        {
            transaction.Status = PaymentStatus.Failed;
            // Keep the rejected attempt pointing at the provider's payment: "why was
            // this refused" is a question someone will ask, and it cannot be answered
            // without the id.
            transaction.ProviderPaymentId = request.ProviderPaymentId ?? transaction.ProviderPaymentId;
            await _repository.UpdateAsync(transaction, ct);
            await ProjectAsync(transaction, LeadEventNames.PaymentRejected, ct);
            _logger.LogInformation("Payment {TransactionId} marked failed", transaction.Id);
            return new ConfirmPaymentResult(Found: true, Published: false, Status: "failed");
        }

        var confirmedAt = DateTime.UtcNow;
        if (!await _repository.TryMarkConfirmedAsync(
                transaction.Id, confirmedAt, request.ProviderPaymentId, ct))
        {
            // Another path confirmed it between our read and this write. With a
            // webhook and two reconciliation paths in play that is a real race,
            // not a theoretical one.
            return new ConfirmPaymentResult(Found: true, Published: false, Status: "already_confirmed");
        }

        transaction.Status = PaymentStatus.Confirmed;
        transaction.ConfirmedAt = confirmedAt;

        await _publisher.PublishAsync(
            new EmissionMessage(
                transaction.Id,
                transaction.BudgetId,
                transaction.CompanyToken,
                transaction.SessionId,
                transaction.Amount,
                confirmedAt),
            ct);

        await ProjectAsync(transaction, LeadEventNames.PaymentConfirmed, ct);

        _logger.LogInformation("Payment {TransactionId} confirmed; emission message published", transaction.Id);
        return new ConfirmPaymentResult(Found: true, Published: true, Status: "confirmed");
    }

    /// <summary>Records the payment outcome on the lead the transaction came from.</summary>
    private Task ProjectAsync(PaymentTransaction transaction, string eventName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(transaction.FlowId))
            return Task.CompletedTask;

        return _leads.ProjectAsync(
            new LeadEvent(
                eventName,
                transaction.FlowId,
                transaction.CompanyToken,
                transaction.SessionId,
                Payload: null,
                DateTime.UtcNow),
            ct);
    }
}
