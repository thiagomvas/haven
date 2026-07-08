using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EnvironmentId)
            .HasColumnName("environment_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.Alias)
            .HasColumnName("alias")
            .HasMaxLength(8);

        builder.HasIndex(nameof(Service.EnvironmentId), nameof(Service.Alias))
            .IsUnique()
            .HasFilter("\"alias\" IS NOT NULL");

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.ExposureMode)
            .HasColumnName("exposure_mode")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.Token)
            .HasColumnName("token")
            .IsRequired();

        builder.Property(x => x.SourceConfigJson)
            .HasColumnName("source_config")
            .HasColumnType("TEXT");

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Ignore(x => x.SourceConfig);

        builder.HasMany(x => x.ServiceNetworks)
            .WithOne()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.ServiceNetworks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}