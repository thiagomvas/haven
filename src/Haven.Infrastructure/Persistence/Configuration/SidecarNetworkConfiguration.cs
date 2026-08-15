using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class SidecarNetworkConfiguration : IEntityTypeConfiguration<SidecarNetwork>
{
    public void Configure(EntityTypeBuilder<SidecarNetwork> builder)
    {
        builder.ToTable("sidecar_networks");

        builder.HasKey(x => new { x.SidecarId, x.NetworkId });

        builder.Property(x => x.SidecarId)
            .HasColumnName("sidecar_id")
            .IsRequired();

        builder.Property(x => x.NetworkId)
            .HasColumnName("network_id")
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.HasOne(x => x.Sidecar)
            .WithMany(s => s.SidecarNetworks)
            .HasForeignKey(x => x.SidecarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Network)
            .WithMany(n => n.SidecarNetworks)
            .HasForeignKey(x => x.NetworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
