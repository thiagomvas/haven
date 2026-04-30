using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Environments.Events;

public sealed class CreateNetworkOnEnvironmentCreatedEventHandler : INotificationHandler<EnvironmentCreatedEvent>
{
    public ValueTask Handle(EnvironmentCreatedEvent notification, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
