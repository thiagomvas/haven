using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Services.Commands.ImportFromManifest;

public sealed class ImportFromManifestHandler(IManifestParser<ServiceManifestDto> parser, IEnvironmentRepository environmentRepository, IServiceRepository serviceRepository) : ICommandHandler<ImportFromManifestCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(ImportFromManifestCommand command, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(command.EnvironmentId, cancellationToken);
        if (environment is null) return Error.NotFoundFor(nameof(Environment), command.EnvironmentId);
        Service service;
        if (!string.IsNullOrWhiteSpace(command.RawManifest))
        {
            var manifest = await parser.ParseAsync(command.RawManifest, cancellationToken);
            manifest.Id = Guid.CreateVersion7();
            foreach (var volume in manifest.Volumes)
                volume.Id = Guid.CreateVersion7();
            service = manifest.ToEntity(environment);
        }
        else
        {
            return Error.InvalidOperation("Raw manifest is required for import.");
        }

        service.RegenerateToken();

        var canCreate = await serviceRepository.CanCreateAsync(service, cancellationToken);
        if (!canCreate.IsSuccess) return canCreate.Error;

        await serviceRepository.AddAsync(service, cancellationToken);

        return service.Id;
    }
}