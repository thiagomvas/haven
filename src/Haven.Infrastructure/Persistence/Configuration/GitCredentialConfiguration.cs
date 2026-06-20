using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class GitCredentialConfiguration : IEntityTypeConfiguration<GitCredentials>
{
    public void Configure(EntityTypeBuilder<GitCredentials> builder)
    {
        builder.ToTable("git_credentials");

        builder.HasKey(gc => gc.Id);
        builder.Property(gc => gc.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(gc => gc.ProviderType)
            .HasColumnName("provider_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(gc => gc.HostUrl)
            .HasColumnName("host_url");

        builder.Property(gc => gc.AuthMethod)
            .HasColumnName("auth_method")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(gc => gc.PrimaryCredential)
            .HasColumnName("primary_credential")
            .IsRequired();

        builder.Property(gc => gc.SecondaryCredential)
            .HasColumnName("secondary_credential");

        builder.Property(gc => gc.WebhookSecret)
            .HasColumnName("webhook_secret");

        builder.Property(gc => gc.DisplayName)
            .HasColumnName("display_name")
            .IsRequired();

        builder.Property(gc => gc.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(gc => gc.LastValidatedAt)
            .HasColumnName("last_validated_at")
            .IsRequired();
    }
}