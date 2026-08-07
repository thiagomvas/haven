using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
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

        builder.HasMany(ncc => ncc.NotificationRules)
            .WithOne(nr => nr.ChannelConfig)
            .HasForeignKey(nr => nr.ChannelConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}