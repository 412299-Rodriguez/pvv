namespace PvvBff.Application.Abstractions;

/// <summary>Vehicle data as the BFF needs it (subset of soat's VehicleDto).</summary>
public sealed record SoatVehicle(string Plate, string Brand, string Model, int Year, string VehicleType);

/// <summary>Active policy data (subset of soat's PolicyDto).</summary>
public sealed record SoatPolicy(string PolicyNumber, DateTime EndDate);

/// <summary>Typed client over pvv-soat for the aggregation handlers.</summary>
public interface ISoatGateway
{
    Task<SoatVehicle?> GetVehicleByPlateAsync(string plate, CancellationToken ct);

    Task<SoatPolicy?> GetActivePolicyByPlateAsync(string plate, CancellationToken ct);
}
