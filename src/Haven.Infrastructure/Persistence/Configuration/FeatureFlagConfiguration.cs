using Haven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");
        
        builder.HasKey(ff => ff.Id);
        builder.Property(ff => ff.Id)
            .HasColumnName("id");
        
        builder.Property(ff => ff.Name)
            .HasColumnName("name")
            .IsRequired();
        
        builder.Property(ff => ff.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(ff => ff.Description)
            .HasColumnName("description");
        
        builder.Property(ff => ff.Value)
            .HasColumnName("value")
            .IsRequired();
        
        builder.Property(ff => ff.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();
        
        builder.HasOne(ff => ff.Service)
            .WithMany(s => s.FeatureFlags)
            .HasForeignKey(ff => ff.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}