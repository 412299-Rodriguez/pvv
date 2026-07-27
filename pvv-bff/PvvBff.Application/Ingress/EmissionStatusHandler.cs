using System.Text.Json;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Internal ingress handler for <c>internal://emission-status</c> (hash
/// EMISSION_STATUS). The front polls this after the payment redirect: it returns
/// the payment + emission state and the denormalized ticket fields so the result
/// page can render without any prior session state.
/// </summary>
public sealed class EmissionStatusHandler : IInternalIngressHandler
{
    private readonly IPaymentRepository _repository;
    private readonly ISoatGateway _soat;
    private readonly ILeadProjectionService _leads;

    public EmissionStatusHandler(
        IPaymentRepository repository,
        ISoatGateway soat,
        ILeadProjectionService leads)
    {
        _repository = repository;
        _soat = soat;
        _leads = leads;
    }

    public string Key => "emission-status";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        var transactionId = ReadString(context.Body, "transactionId");
        if (string.IsNullOrWhiteSpace(transactionId))
            return IngressResponse.Failure(400, "Falta transactionId.");

        var tx = await _repository.GetAsync(transactionId, ct);
        if (tx is null)
            return IngressResponse.Failure(404, "Transacción no encontrada.");

        // Funnel step 5. This poll is the BFF's first chance to see the worker's
        // outcome, so the lead is closed here — the browser is not trusted with it.
        await ProjectEmissionOutcomeAsync(tx, ct);

        // Once emitted, fetch the real coverage window from soat (it may be
        // future-dated for a renewal, so we can't compute it on the front).
        DateTime? validFrom = null;
        DateTime? validUntil = null;
        if (!string.IsNullOrWhiteSpace(tx.PolicyNumber))
        {
            var dates = await _soat.GetPolicyByNumberAsync(tx.PolicyNumber, ct);
            if (dates is not null)
            {
                validFrom = dates.StartDate;
                validUntil = dates.EndDate;
            }
        }

        return IngressResponse.Success(200, new
        {
            transactionId,
            paymentStatus = tx.Status.ToString(),
            emissionStatus = tx.EmissionStatus ?? "pending",
            policyNumber = tx.PolicyNumber,
            amount = tx.Amount,
            vehicleTitle = tx.VehicleTitle,
            holderName = tx.HolderName,
            validFrom,
            validUntil,
            emissionUpdatedAt = tx.EmissionUpdatedAt,
        });
    }

    /// <summary>
    /// Closes the funnel once the worker has reached a terminal emission state.
    /// Stamped on the transaction so the polling never projects it twice.
    /// </summary>
    private async Task ProjectEmissionOutcomeAsync(Domain.Payments.PaymentTransaction tx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tx.FlowId) || tx.EmissionProjectedAt is not null)
            return;

        var eventName = tx.EmissionStatus switch
        {
            "success" => LeadEventNames.PolicyIssued,
            "failed" or "retry-exhausted" => LeadEventNames.EmissionFailed,
            _ => null,
        };

        if (eventName is null)
            return;

        // Claim BEFORE projecting. The result page polls, and two polls in flight
        // together would both pass the check above; only one wins the write.
        if (!await _repository.TryClaimEmissionProjectionAsync(tx.Id, DateTime.UtcNow, ct))
            return;

        await _leads.ProjectAsync(
            new LeadEvent(
                eventName,
                tx.FlowId,
                tx.CompanyToken,
                tx.SessionId,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["policyNumber"] = tx.PolicyNumber,
                },
                DateTime.UtcNow),
            ct);
    }

    private static string ReadString(JsonElement? body, string key) =>
        body is { ValueKind: JsonValueKind.Object } obj &&
        obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
