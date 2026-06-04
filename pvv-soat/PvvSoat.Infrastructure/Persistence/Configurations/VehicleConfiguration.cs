using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvSoat.Domain.Entities;

namespace PvvSoat.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.VehicleId);

        builder.HasIndex(v => v.Plate).IsUnique();
        builder.Property(v => v.Plate).HasMaxLength(10).IsRequired();
        builder.Property(v => v.Brand).HasMaxLength(60).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(60).IsRequired();
        builder.Property(v => v.Year).IsRequired();
        builder.Property(v => v.VehicleType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.CreatedAt).IsRequired();

        builder.HasMany(v => v.Budgets).WithOne(b => b.Vehicle);
        builder.HasMany(v => v.Policies).WithOne(p => p.Vehicle);
    }
}
