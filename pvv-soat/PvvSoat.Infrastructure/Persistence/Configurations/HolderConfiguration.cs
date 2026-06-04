using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PvvSoat.Domain.Entities;

namespace PvvSoat.Infrastructure.Persistence.Configurations;

public class HolderConfiguration : IEntityTypeConfiguration<Holder>
{
    public void Configure(EntityTypeBuilder<Holder> builder)
    {
        builder.HasKey(h => h.HolderId);

        builder.HasIndex(h => h.DNI).IsUnique();
        builder.Property(h => h.DNI).HasMaxLength(20).IsRequired();
        builder.Property(h => h.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(h => h.LastName).HasMaxLength(100).IsRequired();
        builder.Property(h => h.Email).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Phone).HasMaxLength(30).IsRequired();
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasMany(h => h.Budgets).WithOne(b => b.Holder);
        builder.HasMany(h => h.Policies).WithOne(p => p.Holder);
    }
}
