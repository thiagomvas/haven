using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;

public sealed class DeleteDomainHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    ITraefikDynamicConfigWriter traefikDynamicConfigWriter)
    : ICommandHandler<DeleteDomainCommand>
{
    public async ValueTask<Result> Handle(DeleteDomainCommand command, CancellationToken cancellationToken)
    {
        var entry = command.SidecarId.HasValue
            ? await serviceRegistryEntryRepository.GetForSidecarAsync(command.SidecarId.Value, cancellationToken)
            : await serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId.Value, cancellationToken);
        if (entry is null)
            return Error.NotFoundFor("ServiceRegistryEntry", command.SidecarId.HasValue ? command.SidecarId.Value : command.ServiceId.Value);

        var domain = entry.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        entry.RemoveDomain(domain);

        // The DB-side DomainCertificate row cascade-deletes with the domain (see
        // DomainCertificateConfiguration), but the files materialized for Traefik's file provider
        // need explicit cleanup - best-effort, no-op if none exist.
        await traefikDynamicConfigWriter.RemoveDomainCertificateAsync(domain.Id, cancellationToken);

        return Result.Success();
    }
}