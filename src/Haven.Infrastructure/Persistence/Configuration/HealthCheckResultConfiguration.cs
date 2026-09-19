using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class HealthCheckResultConfiguration : IEntityTypeConfiguration<HealthCheckResult>
{
    public void Configure(EntityTypeBuilder<HealthCheckResult> builder)
    {
        builder.ToTable("health_check_results");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.HealthCheckId)
            .HasColumnName("health_check_id")
            .IsRequired();

        builder.Property(r => r.RanAt)
            .HasColumnName("ran_at")
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Message)
            .HasColumnName("message")
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(r => r.DurationMs)
            .HasColumnName("duration_ms")
            .IsRequired();

        builder.Property(r => r.HttpStatusCode)
            .HasColumnName("http_status_code")
            .IsRequired(false);

        builder.Property(r => r.ExitCode)
            .HasColumnName("exit_code")
            .IsRequired(false);

        builder.Property(r => r.Output)
            .HasColumnName("output")
            .IsRequired(false);

        builder.Property(r => r.Attempts)
            .HasColumnName("attempts")
            .IsRequired();

        builder.HasOne<HealthCheck>()
            .WithMany()
            .HasForeignKey(r => r.HealthCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.HealthCheckId, r.RanAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_health_check_results_check_ran_at");
    }
}
