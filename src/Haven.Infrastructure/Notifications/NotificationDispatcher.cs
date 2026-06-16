using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Notifications;

public sealed class NotificationDispatcher(
    INotificationAttemptRepository attemptRepository,
    IEnumerable<INotificationProvider> providers,
    IUnitOfWork unitOfWork,
    ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    public async Task DispatchAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await attemptRepository.GetByIdAsync(attemptId, ct);
        if (attempt is null)
        {
            logger.LogError("Notification attempt {AttemptId} not found", attemptId);
            return;
        }

        var provider = providers.FirstOrDefault(p => p.Channel == attempt.Channel);
        if (provider is null)
        {
            logger.LogError(
                "No notification provider found for channel {Channel} (attempt {AttemptId})",
                attempt.Channel, attemptId);
            attempt.MarkFailed(string.Empty, null, $"No provider registered for channel '{attempt.Channel}'.");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var config = attempt.Rule?.ChannelConfig;
        if (config is null)
        {
            logger.LogError("Channel config missing for attempt {AttemptId}", attemptId);
            attempt.MarkFailed(string.Empty, null, "Channel configuration not found.");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        try
        {
            var result = await provider.SendAsync(attempt, config, ct);

            if (result.Success)
                attempt.MarkDelivered(result.SentPayload, result.Response);
            else
                attempt.MarkFailed(result.SentPayload, result.Response, result.ErrorMessage ?? "Unknown error.");

            await attemptRepository.UpdateAsync(attempt, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while dispatching attempt {AttemptId}", attemptId);
            attempt.MarkFailed(string.Empty, null, ex.Message);
            await attemptRepository.UpdateAsync(attempt, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}