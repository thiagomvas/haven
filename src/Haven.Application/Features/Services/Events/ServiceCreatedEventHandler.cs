using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Services.Events;

public sealed class ServiceCreatedEventHandler(
    IServiceRepository repository,
    IManifestSerializer serializer) : INotificationHandler<ServiceCreatedEvent>
{
    public async ValueTask Handle(ServiceCreatedEvent notification, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(notification.ServiceId, cancellationToken);
        if (service == null) return;
        await serializer.WriteServiceAsync(service.Environment.Project, service.Environment, service, cancellationToken);
    }
}
