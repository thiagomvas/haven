using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Commands.DeleteVolume;

public sealed class DeleteVolumeHandler(
    IServiceRepository serviceRepository,
    IManagedVolumeFileService managedVolumeFileService)
    : ICommandHandler<DeleteVolumeCommand>
{
    public async ValueTask<Result> Handle(DeleteVolumeCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var volume = service.Volumes.FirstOrDefault(v => v.Id == command.VolumeId);
        if (volume is null)
            return Error.NotFoundFor(nameof(ServiceVolume), command.VolumeId);

        var isManaged = volume.Type == VolumeType.Managed;

        service.RemoveVolume(volume);

        if (isManaged)
        {
            var deleteResult = await managedVolumeFileService.DeleteVolumeDirectoryAsync(
                command.ServiceId, command.VolumeId, cancellationToken);

            if (deleteResult.IsFailure)
                return deleteResult.Error;
        }

        return Result.Success();
    }
}