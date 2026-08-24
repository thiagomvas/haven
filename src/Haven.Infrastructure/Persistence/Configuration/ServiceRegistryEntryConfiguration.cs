using Haven.Domain.Aggregates;
using Haven.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class ServiceRegistryEntryConfiguration : IEntityTypeConfiguration<ServiceRegistryEntry>
{
    public void Configure(EntityTypeBuilder<ServiceRegistryEntry> builder)
    {
        builder.ToTable("service_registry");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.ServiceId)
            .HasColumnName("service_id");

        builder.Property(p => p.SidecarId)
            .HasColumnName("sidecar_id");

        builder.Property(p => p.ContainerName)
            .HasColumnName("container_name")
            .HasMaxLength(255);

        builder.Property(p => p.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.Property(p => p.Ports)
            .HasColumnName("ports")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<PortMapping>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<PortMapping>())
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<PortMapping>());

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(p => p.RegisteredAt)
            .HasColumnName("registered_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(p => p.StartedAt)
            .HasColumnName("started_at");


        builder.HasOne(p => p.Service)
            .WithMany()
            .HasForeignKey(p => p.ServiceId);

        builder.HasOne(p => p.Sidecar)
            .WithMany()
            .HasForeignKey(p => p.SidecarId);

        // Exactly one of ServiceId/SidecarId must be set - mirrors the invariant enforced by
        // ServiceRegistryEntry.Create/CreateForSidecar.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_service_registry_owner",
            "(service_id IS NOT NULL AND sidecar_id IS NULL) OR (service_id IS NULL AND sidecar_id IS NOT NULL)"));
    }
}