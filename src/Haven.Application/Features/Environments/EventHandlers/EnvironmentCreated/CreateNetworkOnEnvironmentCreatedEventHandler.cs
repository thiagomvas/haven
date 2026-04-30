using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Environments.Events;

public sealed class CreateNetworkOnEnvironmentCreatedEventHandler(IMediator mediator, IEnvironmentRepository environmentRepository) : INotificationHandler<EnvironmentCreatedEvent>
{
    public async ValueTask Handle(EnvironmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(notification.EnvironmentId, cancellationToken);
        if (environment is null) return;

        var networkName = Network.CreateProjectEnvironmentNetwork(
            environment.ProjectId,
            environment.Project!.Name,
            environment.Id,
            environment.Name).Name;

        var createNetworkCommand = new CreateNetworkCommand(
            networkName,
            environment.ProjectId,
            environment.Id);

        await mediator.Send(createNetworkCommand, cancellationToken);
    }
}
