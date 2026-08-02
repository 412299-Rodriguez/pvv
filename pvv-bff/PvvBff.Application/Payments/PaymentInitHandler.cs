using System.Text.Json;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Ingress;
using PvvBff.Application.Leads;
using PvvBff.Domain.Payments;

namespace PvvBff.Application.Payments;

/// <summary>
/// Internal ingress handler for <c>internal://payment-init</c> (hash PAYMENT_INIT).
/// Creates a pending transaction and a checkout preference, returning the
/// init_point the front redirects the user to.
/// </summary>
public sealed class PaymentInitHandler : IInternalIngressHandler
{
    private readonly IPaymentGateway _gateway;
    private readonly IPaymentRepository _repository;
    private readonly ILeadProjectionService _leads;

    public PaymentInitHandler(
        IPaymentGateway gateway,
        IPaymentRepository repository,
        ILeadProjectionService leads)
    {
        _gateway = gateway;
        _repository = repository;
        _leads = leads;
    }

    public string Key => "payment-init";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        if (!TryReadRequest(context.Body, out var budgetId, out var amount))
            return IngressResponse.Failure(400, "PAYMENT_INIT requires 'budgetId' and a positive 'amount'.");

        var transactionId = Guid.NewGuid().ToString("N");
        var flowId = ReadOptionalString(context.Body, "flowId");
        var vehicleTitle = ReadOptionalString(context.Body, "vehicleTitle");
        var holderName = ReadOptionalString(context.Body, "holderName");

        // With the real gateway this string is the item title the buyer reads on
        // Mercado Pago's own checkout, so it is written for them. The product name
        // does not reach this handler (PAYMENT_INIT only carries the budget), so
        // the vehicle is what identifies the purchase.
        var description = string.IsNullOrWhiteSpace(vehicleTitle)
            ? "Seguro vehicular"
            : $"Seguro para {vehicleTitle}";

        var preference = await _gateway.CreatePreferenceAsync(
            new PaymentPreferenceRequest(transactionId, amount, description, context.CompanyId), ct);

        await _repository.AddAsync(
            new PaymentTransaction
            {
                Id = transactionId,
                SessionId = context.SessionId,
                CompanyToken = context.CompanyId,
                // Kept so the webhook and the emission can find the lead later.
                FlowId = flowId,
                BudgetId = budgetId,
                Amount = amount,
                PreferenceId = preference.PreferenceId,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                // Denormalized so the result page can show the ticket after the redirect.
                VehicleTitle = vehicleTitle,
                HolderName = holderName,
            },
            ct);

        // Funnel step 4: the visitor reached the checkout. Projected here rather
        // than from the front, which is about to be redirected away.
        if (!string.IsNullOrWhiteSpace(flowId))
        {
            await _leads.ProjectAsync(
                new LeadEvent(
                    LeadEventNames.PaymentInitiated,
                    flowId,
                    context.CompanyId,
                    context.SessionId,
                    new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["paymentMethod"] = "mercadopago",
                        ["transactionId"] = transactionId,
                        ["preferenceId"] = preference.PreferenceId,
                    },
                    DateTime.UtcNow),
                ct);
        }

        return IngressResponse.Success(200, new
        {
            transactionId,
            preferenceId = preference.PreferenceId,
            initPoint = preference.InitPoint,
            amount,
        });
    }

    private static bool TryReadRequest(JsonElement? body, out string budgetId, out decimal amount)
    {
        budgetId = string.Empty;
        amount = 0m;

        if (body is not { ValueKind: JsonValueKind.Object } obj)
            return false;

        if (obj.TryGetProperty("budgetId", out var budget) && budget.ValueKind == JsonValueKind.String)
            budgetId = budget.GetString()!;

        if (obj.TryGetProperty("amount", out var value) && value.ValueKind == JsonValueKind.Number)
            amount = value.GetDecimal();

        return !string.IsNullOrWhiteSpace(budgetId) && amount > 0m;
    }

    private static string? ReadOptionalString(JsonElement? body, string key) =>
        body is { ValueKind: JsonValueKind.Object } obj &&
        obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
