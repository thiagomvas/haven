using Haven.Domain;
using Haven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class EnvironmentVariableConfiguration : IEntityTypeConfiguration<EnvironmentVariables>
{
    public void Configure(EntityTypeBuilder<EnvironmentVariables> builder)
    {
        builder.ToTable("envs");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        
        builder.Property(x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(128)
            .IsRequired();
        
        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasMaxLength(128);
        
        builder.Property(x => x.ParentType)
            .HasColumnName("parent_type")
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<EnvironmentVariableParentType>(v))
            .IsRequired();

        builder.Property(x => x.ParentId)
            .HasColumnName("parent_id")
            .IsRequired();
    }
}