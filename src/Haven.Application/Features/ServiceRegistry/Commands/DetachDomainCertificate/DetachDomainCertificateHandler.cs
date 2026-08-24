using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.ServiceRegistry.Commands.DetachDomainCertificate;

/// <summary>
/// Detaches the domain's currently-attached library certificate (if any). Does not change the
/// domain's TLS mode - a 'Custom' mode domain with no certificate attached is left as a flagged,
/// incomplete state. The library certificate itself is untouched and may still be attached to
/// other domains.
/// </summary>
public sealed class DetachDomainCertificateHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    ITraefikDynamicConfigWriter traefikDynamicConfigWriter)
    : ICommandHandler<DetachDomainCertificateCommand>
{
    public async ValueTask<Result> Handle(DetachDomainCertificateCommand command, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId, cancellationToken);
        if (entry is null)
            return Error.NotFoundFor("ServiceRegistryEntry", command.ServiceId);

        var domain = entry.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        domain.SslCertificateId = null;
        domain.Certificate = null;
        await traefikDynamicConfigWriter.RemoveDomainCertificateAsync(domain.Id, cancellationToken);

        return Result.Success();
    }
}
