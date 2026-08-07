using Haven.Application.Common.Interfaces;
using Haven.Application.Features.NotificationChannels;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Notifications;

/// <summary>
/// One-time, idempotent startup step that encrypts any still-plaintext SMTP passwords left over
/// from before SMTP provider secrets were encrypted at rest. Detection is exact (the
/// <see cref="SmtpConfigJsonCodec.EncryptedMarker"/> prefix), not heuristic, so this is safe to
/// run on every boot — it's a no-op once every row has been migrated.
/// </summary>
public static class SmtpPasswordMigrator
{
    public static async Task EncryptLegacyPasswordsAsync(HavenDbContext context, IEncryptionService encryptionService, ILogger logger, CancellationToken ct = default)
    {
        var smtpConfigs = await context.NotificationChannelConfigs
            .Where(c => c.Channel == NotificationChannel.Smtp)
            .ToListAsync(ct);

        var migratedCount = 0;
        foreach (var config in smtpConfigs)
        {
            try
            {
                var encrypted = SmtpConfigJsonCodec.Encrypt(config.Config, encryptionService);
                if (encrypted == config.Config)
                    continue;

                config.Config = encrypted;
                migratedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to migrate SMTP password encryption for notification channel config {ConfigId}", config.Id);
            }
        }

        if (migratedCount > 0)
        {
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Encrypted {Count} previously-plaintext SMTP provider password(s)", migratedCount);
        }
    }
}