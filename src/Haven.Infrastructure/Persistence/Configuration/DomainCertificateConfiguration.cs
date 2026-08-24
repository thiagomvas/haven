using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class DomainCertificateConfiguration : IEntityTypeConfiguration<DomainCertificate>
{
    public void Configure(EntityTypeBuilder<DomainCertificate> builder)
    {
        builder.ToTable("domain_certificates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ServiceRegistryDomainId)
            .HasColumnName("service_registry_domain_id")
            .IsRequired();

        builder.Property(x => x.CertificatePem)
            .HasColumnName("certificate_pem")
            .IsRequired();

        builder.Property(x => x.PrivateKeyPem)
            .HasColumnName("private_key_pem")
            .IsRequired();

        builder.Property(x => x.NotBefore)
            .HasColumnName("not_before")
            .IsRequired();

        builder.Property(x => x.NotAfter)
            .HasColumnName("not_after")
            .IsRequired();

        builder.Property(x => x.SubjectCommonName)
            .HasColumnName("subject_common_name");

        builder.Property(x => x.Fingerprint)
            .HasColumnName("fingerprint")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.ServiceRegistryDomainId).IsUnique();

        builder.HasOne(x => x.ServiceRegistryDomain)
            .WithOne(d => d.Certificate)
            .HasForeignKey<DomainCertificate>(x => x.ServiceRegistryDomainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
