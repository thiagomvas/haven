using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Commands.WriteVolumeFileContent;

public sealed class WriteVolumeFileContentHandler(
    IServiceRepository serviceRepository,
    IManagedVolumeFileService managedVolumeFileService)
    : ICommandHandler<WriteVolumeFileContentCommand>
{
    public async ValueTask<Result> Handle(WriteVolumeFileContentCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var volumeResult = service.GetManagedVolume(command.VolumeId);
        if (volumeResult.IsFailure)
            return volumeResult.Error;

        if (volumeResult.Value.ReadOnly)
            return Error.InvalidOperation("Cannot modify a read-only volume.");

        return await managedVolumeFileService.WriteFileAsync(command.ServiceId, command.VolumeId, command.Path, command.Content, cancellationToken);
    }
}
