using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.ServiceRegistry.Commands.RemoveDomainCertificate;

public sealed class RemoveDomainCertificateHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    IDomainCertificateRepository domainCertificateRepository,
    ITraefikDynamicConfigWriter traefikDynamicConfigWriter)
    : ICommandHandler<RemoveDomainCertificateCommand>
{
    public async ValueTask<Result> Handle(RemoveDomainCertificateCommand command, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId, cancellationToken);
        if (entry is null)
            return Error.NotFoundFor("ServiceRegistryEntry", command.ServiceId);

        var domain = entry.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        await domainCertificateRepository.RemoveByDomainIdAsync(domain.Id, cancellationToken);
        await traefikDynamicConfigWriter.RemoveDomainCertificateAsync(domain.Id, cancellationToken);

        return Result.Success();
    }
}
