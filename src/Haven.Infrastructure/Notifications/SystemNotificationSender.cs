using Haven.Application.Common;
using Haven.Application.Common.Contracts.Notifications;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.SystemNotifications;
using Haven.Application.Features.NotificationChannels;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Infrastructure.Notifications;

public class SystemNotificationSender(
    INotificationChannelConfigRepository channelConfigRepository,
    IEncryptionService encryptionService)
    : ISystemNotificationSender
{
    public async Task<Result> SendAsync(SystemNotificationType type, string recipientEmail,
        IReadOnlyDictionary<string, string> templateData, CancellationToken cancellationToken = default)
    {
        var config = await channelConfigRepository.GetSystemDefaultAsync(NotificationChannel.Smtp, cancellationToken);
        if (config is null || !config.Enabled)
            return Error.InvalidOperation("No system default SMTP provider is configured.");

        var smtpConfig = config.ToProviderConfig<SmtpNotificationConfig>();
        smtpConfig.Password = SmtpConfigJsonCodec.DecryptPassword(config.Config, encryptionService);

        var (subject, textBody, htmlBody) = SystemNotificationTemplates.Render(type, templateData);

        try
        {
            await MailKitSmtpSender.SendAsync(smtpConfig, [recipientEmail], subject, textBody, htmlBody, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.InvalidOperation(ex.Message);
        }

        return Result.Success();
    }
}