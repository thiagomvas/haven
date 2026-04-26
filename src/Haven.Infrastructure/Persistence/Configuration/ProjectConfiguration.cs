using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
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

            env.WithOwner(e => e.Project)
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

            env.OwnsMany(e => e.Services, svc =>
            {
                svc.ToTable("services");

                svc.WithOwner(s => s.Environment)
                   .HasForeignKey(s => s.EnvironmentId);

                svc.HasKey(s => s.Id);

                svc.Property(s => s.Id)
                   .HasColumnName("id");

                svc.Property(s => s.EnvironmentId)
                   .HasColumnName("environment_id")
                   .IsRequired();

                svc.Property(s => s.Name)
                   .HasColumnName("name")
                   .IsRequired();

                svc.Property(s => s.Type)
                   .HasColumnName("type")
                   .IsRequired();

                svc.Property(s => s.ExposureMode)
                   .HasColumnName("exposure_mode")
                   .IsRequired();

                svc.Property(s => s.Status)
                   .HasColumnName("status")
                   .IsRequired();

                svc.Property(s => s.CreatedAt)
                   .HasColumnName("created_at")
                   .IsRequired();

                svc.Property(s => s.UpdatedAt)
                   .HasColumnName("updated_at")
                   .IsRequired();

                svc.Property(s => s.SourceConfigJson)
                   .HasColumnName("source_config")
                   .HasColumnType("TEXT");

                svc.Ignore(s => s.SourceConfig);

                svc.Navigation(s => s.ServiceNetworks)
                   .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            env.Navigation(e => e.Services)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(x => x.Environments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
