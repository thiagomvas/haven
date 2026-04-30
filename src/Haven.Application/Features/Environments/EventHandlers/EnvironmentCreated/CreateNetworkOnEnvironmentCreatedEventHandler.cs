using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Environments.Events;

public sealed class CreateNetworkOnEnvironmentCreatedEventHandler(IMediator mediator) : INotificationHandler<EnvironmentCreatedEvent>
{
    public async ValueTask Handle(EnvironmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        var networkName = Network.CreateProjectEnvironmentNetwork(
            notification.ProjectId,
            notification.ProjectName,
            notification.EnvironmentId,
            notification.EnvironmentName).Name;

        var createNetworkCommand = new CreateNetworkCommand(
            networkName,
            notification.ProjectId,
            notification.EnvironmentId);

        await mediator.Send(createNetworkCommand, cancellationToken);
    }
}
