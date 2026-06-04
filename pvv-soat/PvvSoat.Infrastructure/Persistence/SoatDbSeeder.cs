using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PvvSoat.Domain.Entities;
using PvvSoat.Domain.Enums;

namespace PvvSoat.Infrastructure.Persistence;

/// <summary>
/// Seeds example data for local development only. Runs when the environment is
/// Development and the Vehicles table is empty.
/// </summary>
public static class SoatDbSeeder
{
    public static async Task SeedAsync(
        SoatDbContext context, IHostEnvironment environment, CancellationToken ct = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (await context.Vehicles.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;

        context.Vehicles.AddRange(
            new Vehicle
            {
                VehicleId = Guid.NewGuid(),
                Plate = "AA001BB",
                Brand = "Toyota",
                Model = "Corolla",
                Year = 2021,
                VehicleType = VehicleType.Car,
                CreatedAt = now
            },
            new Vehicle
            {
                VehicleId = Guid.NewGuid(),
                Plate = "AC123BD",
                Brand = "Volkswagen",
                Model = "Amarok",
                Year = 2023,
                VehicleType = VehicleType.Truck,
                CreatedAt = now
            },
            new Vehicle
            {
                VehicleId = Guid.NewGuid(),
                Plate = "MNO456",
                Brand = "Honda",
                Model = "CG Titan",
                Year = 2019,
                VehicleType = VehicleType.Motorcycle,
                CreatedAt = now
            });

        context.Holders.AddRange(
            new Holder
            {
                HolderId = Guid.NewGuid(),
                DNI = "30111222",
                FirstName = "Juan",
                LastName = "Perez",
                Email = "juan.perez@example.com",
                Phone = "3510000001",
                CreatedAt = now
            },
            new Holder
            {
                HolderId = Guid.NewGuid(),
                DNI = "28999888",
                FirstName = "Maria",
                LastName = "Gomez",
                Email = "maria.gomez@example.com",
                Phone = "3510000002",
                CreatedAt = now
            });

        await context.SaveChangesAsync(ct);
    }
}
