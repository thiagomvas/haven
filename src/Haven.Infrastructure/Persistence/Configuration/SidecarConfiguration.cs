using Haven.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class SidecarConfiguration : IEntityTypeConfiguration<Sidecar>
{
    public void Configure(EntityTypeBuilder<Sidecar> builder)
    {
        builder.ToTable("sidecars");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.Alias)
            .HasColumnName("alias")
            .HasMaxLength(8);

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.Health)
            .HasColumnName("health")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Enabled)
            .HasColumnName("enabled")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.LastDeployedAt)
            .HasColumnName("last_deployed_at");

        builder.Property(x => x.SourceConfigJson)
            .HasColumnName("source_config")
            .HasColumnType("TEXT");

        builder.Ignore(x => x.SourceConfig);

        builder.HasMany(x => x.SidecarNetworks)
            .WithOne()
            .HasForeignKey(x => x.SidecarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.SidecarNetworks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
