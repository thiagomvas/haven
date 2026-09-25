using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class SecretVariableConfiguration : IEntityTypeConfiguration<SecretVariable>
{
    public void Configure(EntityTypeBuilder<SecretVariable> builder)
    {
        builder.ToTable("secrets");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.ParentId)
            .HasColumnName("parent_id")
            .IsRequired();

        builder.Property(x => x.ParentType)
            .HasColumnName("parent_type")
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<EnvironmentVariableParentType>(v))
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnName("value");

        builder.HasIndex(x => new { x.ParentId, x.ParentType, x.Key })
            .IsUnique();
    }
}