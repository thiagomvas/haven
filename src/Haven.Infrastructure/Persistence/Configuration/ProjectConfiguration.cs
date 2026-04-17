using Haven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HavenEnvironment = Haven.Domain.Entities.Environment;

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

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(Project.MaxDescriptionLength);

        builder.OwnsMany(x => x.Environments, env =>
        {
            env.ToTable("environments");

            env.WithOwner()
               .HasForeignKey(e => e.ProjectId);

            env.HasKey(e => e.Id);

            env.Property(e => e.Id)
               .HasColumnName("id");

            env.Property(e => e.ProjectId)
               .HasColumnName("project_id")
               .IsRequired();

            env.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(HavenEnvironment.MaxNameLength);

            env.Property(e => e.Description)
               .HasColumnName("description")
               .HasMaxLength(HavenEnvironment.MaxDescriptionLength);

            env.Property(e => e.NetworkName)
               .HasColumnName("network_name")
               .IsRequired();
        });

        builder.Navigation(x => x.Environments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
