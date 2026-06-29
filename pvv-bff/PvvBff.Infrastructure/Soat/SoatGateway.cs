using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Soat;

/// <summary>Typed HttpClient over pvv-soat used by the aggregation handlers.</summary>
public sealed class SoatGateway : ISoatGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public SoatGateway(HttpClient http) => _http = http;

    public async Task<SoatVehicle?> GetVehicleByPlateAsync(string plate, CancellationToken ct)
    {
        using var response = await _http.GetAsync($"/api/vehicles/{Uri.EscapeDataString(plate)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<VehicleResponse>(JsonOptions, ct);
        return dto is null
            ? null
            : new SoatVehicle(dto.VehicleId, dto.Plate, dto.Brand, dto.Model, dto.Year, dto.VehicleType);
    }

    public async Task<SoatPolicy?> GetActivePolicyByPlateAsync(string plate, CancellationToken ct)
    {
        using var response = await _http.GetAsync($"/api/policies/active/{Uri.EscapeDataString(plate)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PolicyResponse>(JsonOptions, ct);
        return dto is null ? null : new SoatPolicy(dto.PolicyNumber, dto.EndDate);
    }

    public async Task<Guid?> GetHolderIdByDniAsync(string dni, CancellationToken ct)
    {
        using var response = await _http.GetAsync($"/api/holders/{Uri.EscapeDataString(dni)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<HolderResponse>(JsonOptions, ct);
        return dto?.HolderId;
    }

    public async Task<Guid> CreateHolderAsync(SoatHolderInput input, CancellationToken ct)
    {
        // soat's CreateHolderCommand is { DNI, FirstName, LastName, Email, Phone };
        // its model binder is case-insensitive, so camelCase keys bind fine.
        var body = new
        {
            Dni = input.Dni,
            input.FirstName,
            input.LastName,
            input.Email,
            input.Phone,
        };
        using var response = await _http.PostAsJsonAsync("/api/holders", body, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<HolderResponse>(JsonOptions, ct);
        return dto!.HolderId;
    }

    public async Task<Guid> CreateBudgetAsync(SoatBudgetInput input, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("/api/budgets", input, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<BudgetResponse>(JsonOptions, ct);
        return dto!.BudgetId;
    }

    public async Task<SoatPolicyDates?> GetPolicyByNumberAsync(string policyNumber, CancellationToken ct)
    {
        using var response = await _http.GetAsync($"/api/policies/{Uri.EscapeDataString(policyNumber)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PolicyDatesResponse>(JsonOptions, ct);
        return dto is null ? null : new SoatPolicyDates(dto.StartDate, dto.EndDate);
    }

    private sealed record VehicleResponse(Guid VehicleId, string Plate, string Brand, string Model, int Year, string VehicleType);

    private sealed record PolicyDatesResponse(DateTime StartDate, DateTime EndDate);

    private sealed record PolicyResponse(string PolicyNumber, DateTime EndDate);

    private sealed record HolderResponse(Guid HolderId);

    private sealed record BudgetResponse(Guid BudgetId);
}
