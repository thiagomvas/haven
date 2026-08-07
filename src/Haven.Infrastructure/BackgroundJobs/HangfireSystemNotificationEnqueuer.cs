using Hangfire;
using Hangfire.States;

using Haven.Application.Common.Interfaces.SystemNotifications;
using Haven.Domain;
using Haven.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HangfireSystemNotificationEnqueuer(
    IBackgroundJobClient backgroundJobClient,
    ILogger<HangfireSystemNotificationEnqueuer> logger)
    : ISystemNotificationEnqueuer
{
    private const string SystemNotificationsQueueName = "system-notifications";

    public Task EnqueueAsync(SystemNotificationType type, string recipientEmail,
        IReadOnlyDictionary<string, string> templateData, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string>(templateData);

        backgroundJobClient.Create<SystemNotificationDispatchJob>(
            x => x.ExecuteAsync(type, recipientEmail, data, CancellationToken.None),
            new EnqueuedState(SystemNotificationsQueueName));

        logger.LogInformation("Enqueued system notification {Type} for {Recipient}", type, recipientEmail);

        return Task.CompletedTask;
    }
}
