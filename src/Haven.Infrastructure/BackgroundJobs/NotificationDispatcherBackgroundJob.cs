using Haven.Application.Common.Interfaces.Notifications;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class NotificationDispatcherBackgroundJob(
    INotificationDispatcher dispatcher,
    ILogger<NotificationDispatcherBackgroundJob> logger)
{
    public async Task ExecuteAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Dispatching notification attempt {AttemptId}", attemptId);

        await dispatcher.DispatchAsync(attemptId, cancellationToken);

        logger.LogInformation("Completed notification attempt {AttemptId}", attemptId);
    }
}