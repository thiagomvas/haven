using Haven.Domain.Aggregates;
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
            .HasColumnName("service_id")
            .IsRequired();
        
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
                v => System.Text.Json.JsonSerializer.Deserialize<List<int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<int>())
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<int>());
        
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired();
        
        builder.Property(p => p.RegisteredAt)
            .HasColumnName("registered_at")
            .IsRequired();
        
        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
        
        builder.HasOne(p => p.Service)
            .WithMany()
            .HasForeignKey(p => p.ServiceId);

    }
}