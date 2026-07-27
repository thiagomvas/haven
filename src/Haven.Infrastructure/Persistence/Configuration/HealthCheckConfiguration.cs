using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class HealthCheckConfiguration : IEntityTypeConfiguration<HealthCheck>
{
    public void Configure(EntityTypeBuilder<HealthCheck> builder)
    {
        builder.ToTable("health_checks");
        
        builder.HasKey(hc => hc.Id);
        builder.Property(hc => hc.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        
        builder.Property(hc => hc.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(hc => hc.Enabled)
            .HasColumnName("enabled")
            .IsRequired();
        
        builder.Property(hc => hc.CronExpression)
            .HasColumnName("cron_expression")
            .IsRequired(false)
            .HasMaxLength(100);
        
        builder.Property(hc => hc.LastRunAt)
            .HasColumnName("last_run_at")
            .IsRequired(false);
        
        builder.Property(hc => hc.Config)
            .HasColumnName("config")
            .IsRequired(false);
        
        builder.Property(hc => hc.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .IsRequired();
        
        builder.Property(hc => hc.LastRunStatus)
            .HasColumnName("last_run_status")
            .HasConversion<string>()
            .IsRequired();
        
        builder.Property(hc => hc.ServiceId)
            .IsRequired()
            .HasColumnName("service_id");
        
        builder.HasOne(hc => hc.Service)
            .WithMany(s => s.HealthChecks)
            .HasForeignKey(hc => hc.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}