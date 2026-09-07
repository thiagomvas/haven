using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Commands.AddDomain;

public sealed class AddDomainHandler(
    IServiceRepository serviceRepository,
    ISidecarRepository sidecarRepository,
    IServiceRegistry serviceRegistry,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository)
    : ICommandHandler<AddDomainCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(AddDomainCommand command, CancellationToken cancellationToken)
    {
        var normalizedHostname = command.Hostname.Trim().ToLowerInvariant();
        if (await serviceRegistryEntryRepository.HostnameExistsAsync(normalizedHostname, excludingDomainId: null, cancellationToken))
            return Error.ConflictFor("Domain hostname", normalizedHostname);

        if (command.SidecarId.HasValue)
        {
            var sidecar = await sidecarRepository.GetByIdAsync(command.SidecarId.Value, cancellationToken);
            if (sidecar is null)
                return Error.NotFoundFor("Sidecar", command.SidecarId.Value);
            if (sidecar.Kind != SidecarKind.Traefik)
                return Error.Validation("Only the Traefik sidecar's dashboard can have a domain assigned.");

            var sidecarEntry = await serviceRegistry.EnsureSidecarRegisteredAsync(command.SidecarId.Value, cancellationToken);
            var sidecarDomain = sidecarEntry.AddDomain(command.Hostname, command.ContainerPort, command.TlsMode);
            return Result<Guid>.CreatedFor(sidecarDomain.Id);
        }

        var service = await serviceRepository.GetByIdAsync(command.ServiceId.Value, cancellationToken);
        if (service is null)
            return Error.NotFoundFor("Service", command.ServiceId.Value);

        var entry = await serviceRegistry.EnsureServiceRegisteredAsync(command.ServiceId.Value, cancellationToken);
        var domain = entry.AddDomain(command.Hostname, command.ContainerPort, command.TlsMode, command.InternalBasePath);

        return Result<Guid>.CreatedFor(domain.Id);
    }
}