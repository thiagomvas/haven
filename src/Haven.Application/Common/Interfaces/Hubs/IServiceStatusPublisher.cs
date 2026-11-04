namespace Haven.Application.Common.Interfaces.Hubs;

/// <summary>
/// Recomputes a service's full state from the database and pushes it to subscribed clients.
/// Never throws: a failed push must not break the operation that triggered it.
/// </summary>
public interface IServiceStatusPublisher
{
    Task PublishAsync(Guid serviceId, CancellationToken cancellationToken = default);
}
