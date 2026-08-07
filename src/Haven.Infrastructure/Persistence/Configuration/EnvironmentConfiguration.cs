using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Persistence.Configuration;

public class
    EnvironmentConfiguration : IEntityTypeConfiguration<Environment>
{
    public void Configure(EntityTypeBuilder<Environment> builder)
    {
        builder.ToTable("environments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(Environment.MaxNameLength);

        builder.Property(x => x.Alias)
            .HasColumnName("alias")
            .HasMaxLength(8);

        builder.HasIndex(nameof(Environment.ProjectId), nameof(Environment.Alias))
            .IsUnique()
            .HasFilter("\"alias\" IS NOT NULL");

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(Environment.MaxDescriptionLength);

        builder.Property(x => x.NetworkName)
            .HasColumnName("network_name")
            .IsRequired();

        builder.HasMany(x => x.Services)
            .WithOne(s => s.Environment)
            .HasForeignKey(s => s.EnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Services)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}