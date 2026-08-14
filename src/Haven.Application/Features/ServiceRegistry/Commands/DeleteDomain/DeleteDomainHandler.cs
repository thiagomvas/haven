using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;

public sealed class DeleteDomainHandler(
    IServiceRegistryEntryRepository serviceRegistryEntryRepository)
    : ICommandHandler<DeleteDomainCommand>
{
    public async ValueTask<Result> Handle(DeleteDomainCommand command, CancellationToken cancellationToken)
    {
        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId, cancellationToken);
        if (entry is null)
            return Error.NotFoundFor("ServiceRegistryEntry", command.ServiceId);

        var domain = entry.Domains.FirstOrDefault(d => d.Id == command.DomainId);
        if (domain is null)
            return Error.NotFoundFor(nameof(ServiceRegistryDomain), command.DomainId);

        entry.RemoveDomain(domain);

        return Result.Success();
    }
}