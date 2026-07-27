using System.Text.Json;
using MediatR;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Payments;
using PvvBff.Domain.Payments;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Internal ingress handler for <c>internal://payment-sync</c> (hash PAYMENT_SYNC).
///
/// The result page calls this once, the moment the buyer comes back from the
/// checkout, before it starts polling. It asks the provider what actually happened
/// and applies the answer.
///
/// It exists for two independent reasons, and either one alone would justify it:
///
/// - Mercado Pago cannot reach a developer's machine, so in development its webhook
///   is not merely late, it never arrives. Without this the sale would never close.
/// - The buyer returns with the outcome in the query string
///   (<c>?collection_status=approved</c>), and a query string can be edited by hand.
///   A payment can therefore only ever be confirmed by asking the provider
///   server-side, never by believing the browser.
/// </summary>
public sealed class PaymentSyncHandler : IInternalIngressHandler
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentGateway _gateway;
    private readonly ISender _mediator;

    public PaymentSyncHandler(
        IPaymentRepository repository,
        IPaymentGateway gateway,
        ISender mediator)
    {
        _repository = repository;
        _gateway = gateway;
        _mediator = mediator;
    }

    public string Key => "payment-sync";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        var transactionId = ReadString(context.Body, "transactionId");
        if (string.IsNullOrWhiteSpace(transactionId))
            return IngressResponse.Failure(400, "Falta transactionId.");

        var tx = await _repository.GetAsync(transactionId, ct);
        // One answer for "does not exist" and "belongs to another portal": telling
        // them apart would turn this into a way to probe for transaction ids.
        if (tx is null || !PaymentAccess.BelongsTo(tx, context.CompanyId))
            return IngressResponse.Failure(404, "Transacción no encontrada.");

        // Already settled: no reason to spend a call on the provider.
        if (tx.Status != PaymentStatus.Pending)
            return Result(transactionId, tx.Status, synced: false, outcome: null);

        var payment = await _gateway.FindPaymentByTransactionAsync(transactionId, ct);
        // No payment on the provider's side yet — or the mock gateway, which has no
        // provider to ask. Either way there is nothing to apply.
        if (payment is null)
            return Result(transactionId, tx.Status, synced: false, outcome: null);

        var result = await _mediator.Send(
            new ConfirmPaymentCommand(
                transactionId, payment.Outcome, payment.ProviderPaymentId, payment.ExpiresAt),
            ct);

        // Re-read rather than assume: the command may have lost the compare-and-set
        // to a webhook that landed at the same moment.
        var updated = await _repository.GetAsync(transactionId, ct);
        return Result(transactionId, updated?.Status ?? tx.Status, synced: true, result.Status);
    }

    private static IngressResponse Result(string transactionId, PaymentStatus status, bool synced, string? outcome) =>
        IngressResponse.Success(200, new
        {
            transactionId,
            paymentStatus = status.ToString(),
            synced,
            outcome,
        });

    private static string ReadString(JsonElement? body, string key) =>
        body is { ValueKind: JsonValueKind.Object } obj &&
        obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
