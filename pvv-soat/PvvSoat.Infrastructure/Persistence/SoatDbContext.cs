using Microsoft.EntityFrameworkCore;
using PvvSoat.Domain.Entities;

namespace PvvSoat.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the pvv-soat domain (database: pvv_soat_db).
/// </summary>
public class SoatDbContext : DbContext
{
    public SoatDbContext(DbContextOptions<SoatDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Holder> Holders => Set<Holder>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SoatDbContext).Assembly);
    }
}
