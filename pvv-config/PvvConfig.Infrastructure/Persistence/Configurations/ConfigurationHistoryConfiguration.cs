using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvConfig.Domain.Entities;

namespace PvvConfig.Infrastructure.Persistence.Configurations;

public class ConfigurationHistoryConfiguration : IEntityTypeConfiguration<ConfigurationHistory>
{
    public void Configure(EntityTypeBuilder<ConfigurationHistory> builder)
    {
        builder.HasKey(h => h.HistoryId);

        builder.Property(h => h.ConfigurationType).HasMaxLength(100).IsRequired();
        builder.Property(h => h.ValueBefore).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(h => h.ValueAfter).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(h => h.ChangedBy).HasMaxLength(200).IsRequired();
        builder.Property(h => h.ChangedAt).IsRequired();

        // Relationship to Configuration is configured in ConfigurationConfiguration.
    }
}
