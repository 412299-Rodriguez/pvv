using Microsoft.EntityFrameworkCore;
using PvvConfig.Domain.Entities;

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

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Configuration> Configurations => Set<Configuration>();
    public DbSet<ConfigurationHistory> ConfigurationHistories => Set<ConfigurationHistory>();
    public DbSet<Operator> Operators => Set<Operator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigDbContext).Assembly);
    }
}
