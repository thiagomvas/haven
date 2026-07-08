using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Commands.UpdateVolume;

public sealed class UpdateVolumeHandler(IServiceRepository serviceRepository)
    : ICommandHandler<UpdateVolumeCommand>
{
    public async ValueTask<Result> Handle(UpdateVolumeCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var volume = service.Volumes.FirstOrDefault(v => v.Id == command.VolumeId);
        if (volume is null)
            return Error.NotFoundFor(nameof(ServiceVolume), command.VolumeId);

        service.UpdateVolume(
            volume,
            name: command.Name,
            source: command.Source,
            target: command.Target,
            readOnly: command.ReadOnly.ToOptional(),
            backupEnabled: command.BackupEnabled.ToOptional());

        return Result.Success();
    }
}