using Haven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id");
        
        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Message)
            .HasColumnName("message")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasMaxLength(4000);
        
        builder.Property(e => e.TriggeredAt)
            .HasColumnName("triggered_at")
            .IsRequired();
        
        builder.HasIndex(e => e.TriggeredAt)
            .HasDatabaseName("idx_events_triggered_at");
        
        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("idx_events_event_type");
    }
}