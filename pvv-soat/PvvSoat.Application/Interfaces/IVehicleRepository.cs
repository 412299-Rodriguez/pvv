using PvvSoat.Domain.Entities;

namespace PvvSoat.Application.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken ct);
    Task AddAsync(Vehicle vehicle, CancellationToken ct);
    Task<bool> ExistsAsync(string plate, CancellationToken ct);
    Task<bool> ExistsByIdAsync(Guid vehicleId, CancellationToken ct);
}
