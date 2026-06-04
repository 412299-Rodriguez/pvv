using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvSoat.Domain.Entities;

namespace PvvSoat.Infrastructure.Persistence.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.BudgetId);

        builder.Property(b => b.Price).HasColumnType("decimal(18,2)");
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.ValidUntil).IsRequired();
        builder.Property(b => b.CompanyId).IsRequired();
        builder.Property(b => b.ProductId).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();

        // FK relationships (Restrict to avoid SQL Server multiple cascade paths).
        builder.HasOne(b => b.Vehicle)
            .WithMany(v => v.Budgets)
            .HasForeignKey(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Holder)
            .WithMany(h => h.Budgets)
            .HasForeignKey(b => b.HolderId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1-to-1 (optional) with Policy; the FK lives on Policy.BudgetId.
        builder.HasOne(b => b.Policy)
            .WithOne(p => p.Budget)
            .HasForeignKey<Policy>(p => p.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        // CompanyId and ProductId are plain columns (no FK to another service).
    }
}
