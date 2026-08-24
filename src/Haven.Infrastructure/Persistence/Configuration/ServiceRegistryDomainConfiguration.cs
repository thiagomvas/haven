using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class ServiceRegistryDomainConfiguration : IEntityTypeConfiguration<ServiceRegistryDomain>
{
    public void Configure(EntityTypeBuilder<ServiceRegistryDomain> builder)
    {
        builder.ToTable("service_registry_domains");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ServiceRegistryEntryId)
            .HasColumnName("service_registry_entry_id")
            .IsRequired();

        builder.Property(x => x.Hostname)
            .HasColumnName("hostname")
            .IsRequired();

        builder.Property(x => x.ContainerPort)
            .HasColumnName("container_port")
            .IsRequired();

        builder.Property(x => x.TlsMode)
            .HasColumnName("tls_mode")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SslCertificateId)
            .HasColumnName("ssl_certificate_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Global uniqueness across the whole Haven instance — a future nginx sidecar will route by
        // hostname, so two services can never claim the same one.
        builder.HasIndex(x => x.Hostname).IsUnique();

        builder.HasOne(x => x.ServiceRegistryEntry)
            .WithMany(e => e.Domains)
            .HasForeignKey(x => x.ServiceRegistryEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // A library certificate can be attached to many domains (e.g. a wildcard cert reused across
        // subdomains) - deleting the certificate detaches it from every domain rather than deleting
        // the domains themselves.
        builder.HasOne(x => x.Certificate)
            .WithMany(c => c.Domains)
            .HasForeignKey(x => x.SslCertificateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}