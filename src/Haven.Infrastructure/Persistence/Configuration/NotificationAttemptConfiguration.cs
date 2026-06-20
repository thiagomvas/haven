using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class NotificationAttemptConfiguration : IEntityTypeConfiguration<NotificationAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationAttempt> builder)
    {
        builder.ToTable("notification_attempt");

        builder.HasKey(na => na.Id);
        builder.Property(na => na.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(na => na.EventType)
            .HasColumnName("event_type")
            .IsRequired();

        builder.Property(na => na.EventPayload)
            .HasColumnName("event_payload")
            .IsRequired();

        builder.Property(na => na.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(na => na.Payload)
            .HasColumnName("payload");

        builder.Property(na => na.Response)
            .HasColumnName("response");

        builder.Property(na => na.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(na => na.ErrorMessage)
            .HasColumnName("error_message");

        builder.Property(na => na.AttemptedAt)
            .HasColumnName("attempted_at");
    }
}