using Haven.Application.Common.Contracts.Notifications;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Models;
using Haven.Application.Features.NotificationChannels;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Notifications.Providers;

public class SmtpNotificationProvider(IEncryptionService encryptionService) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Smtp;

    public async Task<NotificationProviderResult> SendAsync(NotificationAttempt attempt, NotificationChannelConfig config, CancellationToken ct = default)
    {
        var smtpConfig = config.ToProviderConfig<SmtpNotificationConfig>();
        smtpConfig.Password = SmtpConfigJsonCodec.DecryptPassword(config.Config, encryptionService);

        var envelope = attempt.CreateEnvelope();

        try
        {
            await MailKitSmtpSender.SendAsync(smtpConfig, smtpConfig.ToEmails, envelope.ToFormattedEventName(), envelope.Message, ct);
        }
        catch (InvalidOperationException ex)
        {
            return new NotificationProviderResult(false, envelope.Message, null, ex.Message);
        }

        return new NotificationProviderResult(true, envelope.Message, null, null);
    }
}
