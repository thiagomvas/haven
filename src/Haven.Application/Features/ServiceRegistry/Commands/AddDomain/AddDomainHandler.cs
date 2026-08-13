using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Commands.AddDomain;

public sealed class AddDomainHandler(
    IServiceRepository serviceRepository,
    IServiceRegistry serviceRegistry,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository)
    : ICommandHandler<AddDomainCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(AddDomainCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor("Service", command.ServiceId);

        var normalizedHostname = command.Hostname.Trim().ToLowerInvariant();
        if (await serviceRegistryEntryRepository.HostnameExistsAsync(normalizedHostname, excludingDomainId: null, cancellationToken))
            return Error.ConflictFor("Domain hostname", normalizedHostname);

        var entry = await serviceRegistry.EnsureServiceRegisteredAsync(command.ServiceId, cancellationToken);
        var domain = entry.AddDomain(command.Hostname, command.ContainerPort);

        return Result<Guid>.CreatedFor(domain.Id);
    }
}
