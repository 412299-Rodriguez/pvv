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

        var corolla = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Plate = "AA001BB",
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2021,
            VehicleType = VehicleType.Car,
            CreatedAt = now
        };
        var amarok = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Plate = "AC123BD",
            Brand = "Volkswagen",
            Model = "Amarok",
            Year = 2023,
            VehicleType = VehicleType.Truck,
            CreatedAt = now
        };
        var titan = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Plate = "MNO456",
            Brand = "Honda",
            Model = "CG Titan",
            Year = 2019,
            VehicleType = VehicleType.Motorcycle,
            CreatedAt = now
        };
        // Extra Car plates with no policy → ready for the normal purchase flow.
        var focus = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Plate = "MDJ345",
            Brand = "Ford",
            Model = "Focus",
            Year = 2020,
            VehicleType = VehicleType.Car,
            CreatedAt = now
        };
        var onix = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Plate = "LRP782",
            Brand = "Chevrolet",
            Model = "Onix",
            Year = 2022,
            VehicleType = VehicleType.Car,
            CreatedAt = now
        };
        var sandero = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Plate = "KQB910",
            Brand = "Renault",
            Model = "Sandero",
            Year = 2018,
            VehicleType = VehicleType.Car,
            CreatedAt = now
        };
        context.Vehicles.AddRange(corolla, amarok, titan, focus, onix, sandero);

        var juan = new Holder
        {
            HolderId = Guid.NewGuid(),
            DNI = "30111222",
            FirstName = "Juan",
            LastName = "Perez",
            Email = "juan.perez@example.com",
            Phone = "3510000001",
            CreatedAt = now
        };
        var maria = new Holder
        {
            HolderId = Guid.NewGuid(),
            DNI = "28999888",
            FirstName = "Maria",
            LastName = "Gomez",
            Email = "maria.gomez@example.com",
            Phone = "3510000002",
            CreatedAt = now
        };
        context.Holders.AddRange(juan, maria);

        // AC123BD (Amarok) already has an ACTIVE policy → exercises the
        // "your vehicle is already insured" flow. AA001BB and MNO456 have none →
        // normal purchase flow.
        var companyId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var budget = new Budget
        {
            BudgetId = Guid.NewGuid(),
            VehicleId = amarok.VehicleId,
            HolderId = maria.HolderId,
            CompanyId = companyId,
            ProductId = productId,
            Price = 89000m,
            ValidUntil = now.AddMinutes(30),
            Status = BudgetStatus.Converted,
            CreatedAt = now
        };
        context.Budgets.Add(budget);

        context.Policies.Add(new Policy
        {
            PolicyId = Guid.NewGuid(),
            PolicyNumber = "PVV-2025-000777",
            BudgetId = budget.BudgetId,
            VehicleId = amarok.VehicleId,
            HolderId = maria.HolderId,
            CompanyId = companyId,
            ProductId = productId,
            Price = 89000m,
            StartDate = now.AddMonths(-2),
            EndDate = now.AddMonths(10),
            Status = PolicyStatus.Active,
            CreatedAt = now
        });

        await context.SaveChangesAsync(ct);
    }
}
