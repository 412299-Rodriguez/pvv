using Microsoft.EntityFrameworkCore;

namespace PvvConfig.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the pvv-config domain (database: pvv_config_db).
/// </summary>
public class ConfigDbContext : DbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options)
        : base(options)
    {
    }

    // TODO Sprint 1: add DbSet<Company>, DbSet<Product>, DbSet<Pricing>,
    //                DbSet<AppearanceConfig>, DbSet<ConfigurationHistory>
    //                and their EF Core configurations.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigDbContext).Assembly);
    }
}
