using Hangfire;

using Haven.Application.Common.Interfaces.SystemNotifications;
using Haven.Domain;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class SystemNotificationDispatchJob(
    ISystemNotificationSender sender,
    ILogger<SystemNotificationDispatchJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(SystemNotificationType type, string recipientEmail,
        Dictionary<string, string> templateData, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending system notification {Type} to {Recipient}", type, recipientEmail);

        var result = await sender.SendAsync(type, recipientEmail, templateData, cancellationToken);

        if (result.IsSuccess)
            logger.LogInformation("Sent system notification {Type} to {Recipient}", type, recipientEmail);
        else
            logger.LogWarning("Failed to send system notification {Type} to {Recipient}: {Error}", type, recipientEmail, result.Error.Message);
    }
}
