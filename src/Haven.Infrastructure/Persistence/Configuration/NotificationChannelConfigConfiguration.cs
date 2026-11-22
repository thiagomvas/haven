using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class NotificationChannelConfigConfiguration : IEntityTypeConfiguration<NotificationChannelConfig>
{
    public void Configure(EntityTypeBuilder<NotificationChannelConfig> builder)
    {
        builder.ToTable("notification_channel_config");

        builder.HasKey(ncc => ncc.Id);
        builder.Property(ncc => ncc.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(ncc => ncc.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(ncc => ncc.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(ncc => ncc.Config)
            .HasColumnName("config")
            .IsRequired();

        builder.Property(ncc => ncc.Enabled)
            .HasColumnName("enabled")
            .IsRequired();

        builder.Property(ncc => ncc.IsSystemDefault)
            .HasColumnName("is_system_default")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ncc => ncc.QuietHours)
            .HasColumnName("quiet_hours")
            .HasConversion(
                quietHours => QuietHoursJson.Serialize(quietHours!),
                json => QuietHoursJson.Deserialize(json),
                new ValueComparer<QuietHours?>(
                    (left, right) => left == right,
                    quietHours => quietHours == null ? 0 : quietHours.GetHashCode(),
                    quietHours => quietHours))
            .HasColumnType("TEXT");

        builder.HasMany(ncc => ncc.NotificationRules)
            .WithOne(nr => nr.ChannelConfig)
            .HasForeignKey(nr => nr.ChannelConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}