using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class ServiceVolumeConfiguration : IEntityTypeConfiguration<ServiceVolume>
{
    public void Configure(EntityTypeBuilder<ServiceVolume> builder)
    {
        builder.ToTable("service_volumes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnName("source");

        builder.Property(x => x.Target)
            .HasColumnName("target")
            .IsRequired();

        builder.Property(x => x.ReadOnly)
            .HasColumnName("read_only")
            .IsRequired();

        builder.Property(x => x.BackupEnabled)
            .HasColumnName("backup_enabled")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(x => x.Service)
            .WithMany(s => s.Volumes)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
