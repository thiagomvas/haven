namespace Haven.Application.Common.Interfaces.Notifications;

public interface INotificationDispatcher
{
    Task DispatchAsync(Guid attemptId, CancellationToken ct = default);
}
