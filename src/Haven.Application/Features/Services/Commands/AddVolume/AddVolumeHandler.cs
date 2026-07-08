using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Commands.AddVolume;

public sealed class AddVolumeHandler(IServiceRepository serviceRepository)
    : ICommandHandler<AddVolumeCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(AddVolumeCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var volume = service.AddVolume(
            command.Type,
            command.Name,
            command.Target,
            command.Source,
            command.ReadOnly,
            command.BackupEnabled);

        return Result<Guid>.CreatedFor(volume.Id);
    }
}