using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;

public sealed class UpdateDomainHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    ITraefikDynamicConfigWriter traefikDynamicConfigWriter)
    : ICommandHandler<UpdateDomainCommand>
{
    public async ValueTask<Result> Handle(UpdateDomainCommand command, CancellationToken cancellationToken)
    {
        var entry = command.SidecarId.HasValue
            ? await serviceRegistryEntryRepository.GetForSidecarAsync(command.SidecarId.Value, cancellationToken)
            : await serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId.Value, cancellationToken);
        if (entry is null)
            return Error.NotFoundFor("ServiceRegistryEntry", command.SidecarId.HasValue ? command.SidecarId.Value : command.ServiceId.Value);

        var domain = entry.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        if (command.Hostname is not null)
        {
            var normalizedHostname = command.Hostname.Trim().ToLowerInvariant();
            if (await serviceRegistryEntryRepository.HostnameExistsAsync(normalizedHostname, excludingDomainId: command.DomainId, cancellationToken))
                return Error.ConflictFor("Domain hostname", normalizedHostname);
        }

        var wasCustom = domain.TlsMode == TlsMode.Custom;

        entry.UpdateDomain(domain, command.Hostname, command.ContainerPort.ToOptional(), command.TlsMode.ToOptional());

        // Leaving Custom mode orphans any attached certificate - detach it (the library certificate
        // itself is untouched and may still be attached elsewhere) and clean up the files
        // materialized for Traefik's file provider so they don't linger.
        if (wasCustom && domain.TlsMode != TlsMode.Custom)
        {
            domain.SslCertificateId = null;
            domain.Certificate = null;
            await traefikDynamicConfigWriter.RemoveDomainCertificateAsync(domain.Id, cancellationToken);
        }

        return Result.Success();
    }
}