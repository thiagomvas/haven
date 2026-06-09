using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(Project.MaxNameLength);

        builder.Property(x => x.Alias)
            .HasColumnName("alias")
            .HasMaxLength(Project.MaxAliasLength);

        builder.HasIndex(x => x.Alias)
            .IsUnique()
            .HasFilter("[alias] IS NOT NULL");

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(Project.MaxDescriptionLength);

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasMany(x => x.Environments)
            .WithOne(e => e.Project)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Environments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}