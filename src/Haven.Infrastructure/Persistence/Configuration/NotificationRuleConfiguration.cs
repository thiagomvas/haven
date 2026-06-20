using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("notification_rule");

        builder.HasKey(nr => nr.Id);
        builder.Property(nr => nr.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(nr => nr.ChannelConfigId)
            .HasColumnName("channel_config_id")
            .IsRequired();

        builder.Property(nr => nr.Scope)
            .HasColumnName("scope")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(nr => nr.ScopeId)
            .HasColumnName("scope_id");

        builder.Property(nr => nr.Enabled)
            .HasColumnName("enabled")
            .IsRequired();

        builder.HasMany(nr => nr.NotificationAttempts)
            .WithOne(na => na.Rule)
            .HasForeignKey(na => na.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}