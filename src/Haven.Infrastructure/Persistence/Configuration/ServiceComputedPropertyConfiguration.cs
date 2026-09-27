using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class ServiceComputedPropertyConfiguration : IEntityTypeConfiguration<ServiceComputedProperty>
{
    public void Configure(EntityTypeBuilder<ServiceComputedProperty> builder)
    {
        builder.ToTable("service_computed_properties");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();

        builder.Property(x => x.Key)
            .HasColumnName("key")
            .IsRequired();

        builder.Property(x => x.Label)
            .HasColumnName("label")
            .IsRequired();

        builder.Property(x => x.Template)
            .HasColumnName("template")
            .IsRequired();

        builder.Property(x => x.IsSecret)
            .HasColumnName("is_secret")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new { x.ServiceId, x.Key })
            .IsUnique();

        builder.HasOne(x => x.Service)
            .WithMany(s => s.ComputedProperties)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}