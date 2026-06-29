namespace PvvBff.Application.Abstractions;

/// <summary>Vehicle data as the BFF needs it (subset of soat's VehicleDto).</summary>
public sealed record SoatVehicle(Guid VehicleId, string Plate, string Brand, string Model, int Year, string VehicleType);

/// <summary>Active policy data (subset of soat's PolicyDto).</summary>
public sealed record SoatPolicy(string PolicyNumber, DateTime EndDate);

/// <summary>A policy's coverage window (for the emission ticket).</summary>
public sealed record SoatPolicyDates(DateTime StartDate, DateTime EndDate);

/// <summary>Holder data to create in soat when the document isn't on file yet.</summary>
public sealed record SoatHolderInput(string Dni, string FirstName, string LastName, string Email, string Phone);

/// <summary>Budget data for soat's POST /api/budgets.</summary>
public sealed record SoatBudgetInput(Guid VehicleId, Guid HolderId, Guid CompanyId, Guid ProductId, decimal Price);

/// <summary>Typed client over pvv-soat for the aggregation handlers.</summary>
public interface ISoatGateway
{
    Task<SoatVehicle?> GetVehicleByPlateAsync(string plate, CancellationToken ct);

    Task<SoatPolicy?> GetActivePolicyByPlateAsync(string plate, CancellationToken ct);

    /// <summary>The holder id for that DNI, or null if no holder exists yet.</summary>
    Task<Guid?> GetHolderIdByDniAsync(string dni, CancellationToken ct);

    /// <summary>Creates (or upserts by DNI) a holder and returns its id.</summary>
    Task<Guid> CreateHolderAsync(SoatHolderInput input, CancellationToken ct);

    /// <summary>Creates a budget and returns its id.</summary>
    Task<Guid> CreateBudgetAsync(SoatBudgetInput input, CancellationToken ct);

    /// <summary>The coverage window of an emitted policy (null if not found).</summary>
    Task<SoatPolicyDates?> GetPolicyByNumberAsync(string policyNumber, CancellationToken ct);
}
