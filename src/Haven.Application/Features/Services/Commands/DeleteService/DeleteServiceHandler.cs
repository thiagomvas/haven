using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Commands.DeleteService;

public sealed class DeleteServiceHandler(
    IServiceRepository repository,
    IServiceRegistryEntryRepository registryEntryRepository) : ICommandHandler<DeleteServiceCommand>
{
    public async ValueTask<Result> Handle(DeleteServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null) return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var registryEntry = await registryEntryRepository.GetForServiceAsync(command.ServiceId, cancellationToken);
        if (registryEntry is not null)
        {
            await registryEntryRepository.DeleteAsync(registryEntry, cancellationToken);
        }

        service.Delete();
        await repository.RemoveAsync(service, cancellationToken);

        return Result.Success();
    }
}