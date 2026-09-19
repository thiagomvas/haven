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

        builder.Property(hc => hc.LastRunReason)
            .HasColumnName("last_run_reason")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(hc => hc.LastRunMessage)
            .HasColumnName("last_run_message")
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(hc => hc.LastRunDurationMs)
            .HasColumnName("last_run_duration_ms")
            .IsRequired(false);

        builder.Property(hc => hc.Retries)
            .HasColumnName("retries")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(hc => hc.FailureThreshold)
            .HasColumnName("failure_threshold")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(hc => hc.SuccessThreshold)
            .HasColumnName("success_threshold")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(hc => hc.ConsecutiveFailures)
            .HasColumnName("consecutive_failures")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(hc => hc.ConsecutiveSuccesses)
            .HasColumnName("consecutive_successes")
            .HasDefaultValue(0)
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