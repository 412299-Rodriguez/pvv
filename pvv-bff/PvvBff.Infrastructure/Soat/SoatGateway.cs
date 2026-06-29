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
        return dto is null ? null : new SoatVehicle(dto.Plate, dto.Brand, dto.Model, dto.Year);
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

    private sealed record VehicleResponse(string Plate, string Brand, string Model, int Year);

    private sealed record PolicyResponse(string PolicyNumber, DateTime EndDate);
}
