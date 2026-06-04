using Microsoft.EntityFrameworkCore;
using PvvSoat.Application.Interfaces;
using PvvSoat.Domain.Entities;
using PvvSoat.Infrastructure.Persistence;

namespace PvvSoat.Infrastructure.Repositories;

public class VehicleRepository(SoatDbContext context) : IVehicleRepository
{
    public Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken ct) =>
        context.Vehicles.FirstOrDefaultAsync(v => v.Plate.ToUpper() == plate.ToUpper(), ct);

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct) =>
        await context.Vehicles.AddAsync(vehicle, ct);

    public Task<bool> ExistsAsync(string plate, CancellationToken ct) =>
        context.Vehicles.AnyAsync(v => v.Plate.ToUpper() == plate.ToUpper(), ct);

    public Task<bool> ExistsByIdAsync(Guid vehicleId, CancellationToken ct) =>
        context.Vehicles.AnyAsync(v => v.VehicleId == vehicleId, ct);
}
