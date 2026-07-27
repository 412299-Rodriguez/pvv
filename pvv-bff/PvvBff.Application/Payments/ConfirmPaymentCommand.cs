using MediatR;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads;
using PvvBff.Domain.Payments;

namespace PvvBff.Application.Payments;

/// <summary>Apply a payment provider notification to a transaction.</summary>
public sealed record ConfirmPaymentCommand(string TransactionId, string Status)
    : IRequest<ConfirmPaymentResult>;

public sealed record ConfirmPaymentResult(bool Found, bool Published, string Status);

/// <summary>
/// Confirms (or fails) a payment from a webhook notification. On the first
/// "approved" notification it marks the transaction Confirmed and publishes an
/// EmissionMessage to RabbitMQ. Idempotent: a repeated approval does NOT
/// re-publish (so the user never gets two policies).
/// </summary>
public sealed class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, ConfirmPaymentResult>
{
    private const string ApprovedStatus = "approved";

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
            _logger.LogWarning("Webhook for unknown transaction {TransactionId}", request.TransactionId);
            return new ConfirmPaymentResult(Found: false, Published: false, Status: "not_found");
        }

        // Idempotency: never publish twice for the same transaction.
        if (transaction.Status == PaymentStatus.Confirmed)
            return new ConfirmPaymentResult(Found: true, Published: false, Status: "already_confirmed");

        if (!string.Equals(request.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            transaction.Status = PaymentStatus.Failed;
            await _repository.UpdateAsync(transaction, ct);
            await ProjectAsync(transaction, LeadEventNames.PaymentRejected, ct);
            _logger.LogInformation("Payment {TransactionId} marked failed ({Status})", transaction.Id, request.Status);
            return new ConfirmPaymentResult(Found: true, Published: false, Status: "failed");
        }

        transaction.Status = PaymentStatus.Confirmed;
        transaction.ConfirmedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(transaction, ct);

        await _publisher.PublishAsync(
            new EmissionMessage(
                transaction.Id,
                transaction.BudgetId,
                transaction.CompanyToken,
                transaction.SessionId,
                transaction.Amount,
                transaction.ConfirmedAt.Value),
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
