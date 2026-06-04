using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvConfig.Domain.Entities;

namespace PvvConfig.Infrastructure.Persistence.Configurations;

public class ConfigurationConfiguration : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.HasKey(c => c.ConfigurationId);

        // Composite index for fast lookups by company + type + active flag.
        builder.HasIndex(c => new { c.CompanyId, c.ConfigurationType, c.IsActive });

        builder.Property(c => c.ConfigurationType).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ConfigurationValue).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.Version).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasMany(c => c.Histories)
            .WithOne(h => h.Configuration)
            .HasForeignKey(h => h.ConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        // CompanyId is a nullable FK (relationship configured in CompanyConfiguration).
    }
}
