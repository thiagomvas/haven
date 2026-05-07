using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Domain;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.EnvironmentVariables.EventHandlers;

public class PersistEnvOnEnvironmentVariableUpdatedEventHandler(IEnvironmentVariableSerializer serializer)
    : INotificationHandler<EnvironmentVariablesUpdatedEvent>
{
    public async ValueTask Handle(EnvironmentVariablesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        switch (notification.Type)
        {
            case EnvironmentVariableParentType.Project:
                await serializer.WriteExampleForProjectAsync(notification.ParentId,
                    cancellationToken);
                break;
            case EnvironmentVariableParentType.Environment:
                await serializer.WriteExampleForEnvironmentAsync(
                    notification.ParentId, cancellationToken);
                break;
            case EnvironmentVariableParentType.Service: await serializer.WriteExampleForServiceAsync(notification.ParentId,
                    cancellationToken);
                break;
            default: throw new InvalidOperationException($"Unknown EnvironmentVariableParentType: {notification.Type}");
        }

        ;
    }
}