using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class ServiceNetworkConfiguration : IEntityTypeConfiguration<ServiceNetwork>
{
    public void Configure(EntityTypeBuilder<ServiceNetwork> builder)
    {
        builder.ToTable("service_networks");

        builder.HasKey(x => new { x.ServiceId, x.NetworkId });

        builder.Property(x => x.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();

        builder.Property(x => x.NetworkId)
            .HasColumnName("network_id")
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.HasOne(x => x.Service)
            .WithMany(s => s.ServiceNetworks)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Network)
            .WithMany(n => n.ServiceNetworks)
            .HasForeignKey(x => x.NetworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}