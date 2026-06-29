using System.Globalization;
using System.Text.Json;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Internal ingress handler for <c>internal://plate-search</c> (hash PLATE_SEARCH).
/// Aggregates two soat calls — the vehicle and its active policy — into the
/// CheckPlateResponse the front expects. An unknown plate is a 404 (the front
/// shows "vehicle not found"); a plate with an active policy drives the
/// "already insured" renew flow.
/// </summary>
public sealed class PlateSearchHandler : IInternalIngressHandler
{
    private readonly ISoatGateway _soat;

    public PlateSearchHandler(ISoatGateway soat) => _soat = soat;

    public string Key => "plate-search";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        var plate = ReadPlate(context.Body);
        if (string.IsNullOrWhiteSpace(plate))
            return IngressResponse.Failure(400, "Falta la patente.");

        var vehicle = await _soat.GetVehicleByPlateAsync(plate, ct);
        if (vehicle is null)
            return IngressResponse.Failure(404, "No encontramos ese vehículo. Verificá la patente.");

        var policy = await _soat.GetActivePolicyByPlateAsync(plate, ct);

        object? existingPolicy = policy is null
            ? null
            : new
            {
                number = policy.PolicyNumber,
                validUntil = policy.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                earliestRenewalIso = policy.EndDate.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            };

        return IngressResponse.Success(200, new
        {
            vehicle = new
            {
                make = vehicle.Brand,
                model = vehicle.Model,
                year = vehicle.Year,
                plate = vehicle.Plate,
            },
            hasActivePolicy = policy is not null,
            existingPolicy,
        });
    }

    private static string ReadPlate(JsonElement? body)
    {
        if (body is { ValueKind: JsonValueKind.Object } obj &&
            obj.TryGetProperty("plate", out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
