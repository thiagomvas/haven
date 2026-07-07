using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Commands.DeleteVolumeFile;

public sealed class DeleteVolumeFileHandler(
    IServiceRepository serviceRepository,
    IManagedVolumeFileService managedVolumeFileService)
    : ICommandHandler<DeleteVolumeFileCommand>
{
    public async ValueTask<Result> Handle(DeleteVolumeFileCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var volume = service.Volumes.FirstOrDefault(v => v.Id == command.VolumeId);
        if (volume is null)
            return Error.NotFoundFor(nameof(ServiceVolume), command.VolumeId);

        if (volume.Type != VolumeType.Managed)
            return Error.InvalidOperation("File operations are only supported for managed volumes.");

        return await managedVolumeFileService.DeleteFileAsync(command.ServiceId, command.VolumeId, command.Path, cancellationToken);
    }
}
