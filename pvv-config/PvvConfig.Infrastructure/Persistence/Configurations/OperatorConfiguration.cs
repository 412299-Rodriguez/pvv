using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvConfig.Domain.Entities;

namespace PvvConfig.Infrastructure.Persistence.Configurations;

public class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.HasKey(o => o.OperatorId);

        builder.HasIndex(o => o.Username).IsUnique();
        builder.Property(o => o.Username).HasMaxLength(100).IsRequired();
        builder.Property(o => o.PasswordHash).IsRequired();
        builder.Property(o => o.IsActive).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        // Relationship to Company is configured in CompanyConfiguration.
    }
}
