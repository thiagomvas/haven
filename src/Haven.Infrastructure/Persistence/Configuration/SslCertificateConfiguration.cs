using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class SslCertificateConfiguration : IEntityTypeConfiguration<SslCertificate>
{
    public void Configure(EntityTypeBuilder<SslCertificate> builder)
    {
        builder.ToTable("ssl_certificates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
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
    }
}
