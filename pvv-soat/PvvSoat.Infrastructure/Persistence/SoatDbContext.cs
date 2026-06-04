using Microsoft.EntityFrameworkCore;

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

    // TODO Sprint 1: add DbSet<Vehicle>, DbSet<Holder>, DbSet<Budget>, DbSet<Policy>
    //                and their EF Core configurations.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SoatDbContext).Assembly);
    }
}
