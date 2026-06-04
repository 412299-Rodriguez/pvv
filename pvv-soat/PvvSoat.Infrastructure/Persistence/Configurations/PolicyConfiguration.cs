using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvSoat.Domain.Entities;

namespace PvvSoat.Infrastructure.Persistence.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.HasKey(p => p.PolicyId);

        builder.HasIndex(p => p.PolicyNumber).IsUnique();
        builder.Property(p => p.PolicyNumber).HasMaxLength(30).IsRequired();

        // 1-to-1 with Budget: a budget converts into exactly one policy.
        builder.HasIndex(p => p.BudgetId).IsUnique();

        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.StartDate).IsRequired();
        builder.Property(p => p.EndDate).IsRequired();
        builder.Property(p => p.CompanyId).IsRequired();
        builder.Property(p => p.ProductId).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        // FK relationships (Restrict to avoid SQL Server multiple cascade paths).
        // The Budget <-> Policy relationship is configured in BudgetConfiguration.
        builder.HasOne(p => p.Vehicle)
            .WithMany(v => v.Policies)
            .HasForeignKey(p => p.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Holder)
            .WithMany(h => h.Policies)
            .HasForeignKey(p => p.HolderId)
            .OnDelete(DeleteBehavior.Restrict);

        // CompanyId and ProductId are plain columns (no FK to another service).
    }
}
