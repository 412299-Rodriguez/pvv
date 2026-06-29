using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Internal ingress handler for <c>internal://budget-calc</c> (hash BUDGET_CALC).
/// Creates the soat budget at pay time, re-resolving the ids in the BFF from the
/// plate + document (no need to thread vehicleId/holderId through the front):
/// looks up the vehicle, finds-or-creates the holder, derives the companyId from
/// the company token, and creates the budget. Returns the budgetId for PAYMENT_INIT.
/// </summary>
public sealed class BudgetCalcHandler : IInternalIngressHandler
{
    private readonly ISoatGateway _soat;

    public BudgetCalcHandler(ISoatGateway soat) => _soat = soat;

    public string Key => "budget-calc";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.CompanyId))
            return IngressResponse.Failure(400, "Falta el token de compañía.");

        if (context.Body is not { ValueKind: JsonValueKind.Object } body)
            return IngressResponse.Failure(400, "Cuerpo inválido.");

        var plate = ReadString(body, "plate");
        var dni = ReadString(body, "dni");
        var productIdRaw = ReadString(body, "productId");
        var price = ReadDecimal(body, "price");

        if (string.IsNullOrWhiteSpace(plate) || string.IsNullOrWhiteSpace(dni) ||
            !Guid.TryParse(productIdRaw, out var productId) || price <= 0m)
        {
            return IngressResponse.Failure(400, "BUDGET_CALC requiere plate, dni, productId y price.");
        }

        var vehicle = await _soat.GetVehicleByPlateAsync(plate, ct);
        if (vehicle is null)
            return IngressResponse.Failure(404, "No encontramos ese vehículo.");

        // Find the holder by DNI, or create it from the form data.
        var holderId = await _soat.GetHolderIdByDniAsync(dni, ct)
            ?? await _soat.CreateHolderAsync(
                new SoatHolderInput(
                    dni,
                    ReadString(body, "firstName"),
                    ReadString(body, "lastName"),
                    ReadString(body, "email"),
                    ReadString(body, "phone")),
                ct);

        var companyId = DeriveCompanyId(context.CompanyId);
        var budgetId = await _soat.CreateBudgetAsync(
            new SoatBudgetInput(vehicle.VehicleId, holderId, companyId, productId, price), ct);

        return IngressResponse.Success(200, new { budgetId = budgetId.ToString(), amount = price });
    }

    /// <summary>
    /// Deterministic companyId derived from the company token. soat doesn't FK-check
    /// it, so a stable per-company Guid is enough to group budgets/policies.
    /// </summary>
    private static Guid DeriveCompanyId(string companyToken)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(companyToken));
        return new Guid(bytes);
    }

    private static string ReadString(JsonElement body, string key) =>
        body.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static decimal ReadDecimal(JsonElement body, string key) =>
        body.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : 0m;
}
