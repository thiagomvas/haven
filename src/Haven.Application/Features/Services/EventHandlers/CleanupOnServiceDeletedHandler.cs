using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;

using Mediator;

using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Services.EventHandlers;

public class CleanupOnServiceDeletedHandler(
    IServiceRepository repository,
    IDeployServiceFactory deployServiceFactory,
    ILogger<CleanupOnServiceDeletedHandler> logger) : INotificationHandler<ServiceDeletedEvent>
{
    public async ValueTask Handle(ServiceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(notification.ServiceId, cancellationToken);
        if (service == null)
        {
            logger.LogWarning("Could not find service '{ServiceId}' for cleanup on deletion", notification.ServiceId);
            return;
        }

        var deployService = deployServiceFactory.Create(service);
        if (deployService == null)
        {
            logger.LogDebug("No deploy service found for service '{ServiceName}', skipping cleanup", service.Name);
            return;
        }

        await deployService.CleanupAsync(service, cancellationToken);
    }
}