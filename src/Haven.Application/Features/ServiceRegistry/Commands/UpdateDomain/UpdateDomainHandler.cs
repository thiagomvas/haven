using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;

public sealed class UpdateDomainHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository)
    : ICommandHandler<UpdateDomainCommand>
{
    public async ValueTask<Result> Handle(UpdateDomainCommand command, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId, cancellationToken);
        if (entry is null)
            return Error.NotFoundFor("ServiceRegistryEntry", command.ServiceId);

        var domain = entry.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        if (command.Hostname is not null)
        {
            var normalizedHostname = command.Hostname.Trim().ToLowerInvariant();
            if (await serviceRegistryEntryRepository.HostnameExistsAsync(normalizedHostname, excludingDomainId: command.DomainId, cancellationToken))
                return Error.ConflictFor("Domain hostname", normalizedHostname);
        }

        entry.UpdateDomain(domain, command.Hostname, command.ContainerPort.ToOptional());

        return Result.Success();
    }
}
