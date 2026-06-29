using System.Text.Json;
using PvvBff.Application.Abstractions;

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

    public EmissionStatusHandler(IPaymentRepository repository, ISoatGateway soat)
    {
        _repository = repository;
        _soat = soat;
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

    private static string ReadString(JsonElement? body, string key) =>
        body is { ValueKind: JsonValueKind.Object } obj &&
        obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
