using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvConfig.Domain.Entities;

namespace PvvConfig.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(c => c.CompanyId);

        builder.HasIndex(c => c.HashedCompanyId).IsUnique();
        builder.Property(c => c.HashedCompanyId).HasMaxLength(500).IsRequired();

        builder.HasIndex(c => c.CUIT).IsUnique();
        builder.Property(c => c.CUIT).HasMaxLength(20).IsRequired();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasMany(c => c.Configurations)
            .WithOne(cfg => cfg.Company)
            .HasForeignKey(cfg => cfg.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Operators)
            .WithOne(o => o.Company)
            .HasForeignKey(o => o.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
