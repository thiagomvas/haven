using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class DeploymentConfiguration : IEntityTypeConfiguration<Domain.Entities.Deployment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Deployment> builder)
    {
        builder.ToTable("deployments");
        
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("id");
        
        builder.Property(d => d.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();
        
        builder.Property(d => d.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();
        
        builder.Property(d => d.FinishedAt)
            .HasColumnName("finished_at");
        
        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.TriggeredBy)
            .HasColumnName("triggered_by");
        
        builder.Property(d => d.LogFile)
            .HasColumnName("log_file")
            .IsRequired();
        
        builder.HasOne(d => d.Service)
            .WithMany(s => s.Deployments)
            .HasForeignKey(d => d.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}