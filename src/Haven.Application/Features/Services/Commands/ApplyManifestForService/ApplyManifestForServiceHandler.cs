using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Services.Commands.ApplyManifestForService;

public sealed class ApplyManifestForServiceHandler(
    IProjectRepository projectRepository,
    IManifestParser<ServiceManifestDto> manifestParser)
    : ICommandHandler<ApplyManifestForServiceCommand>
{
    public async ValueTask<Result> Handle(ApplyManifestForServiceCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var service = environment.Services.FirstOrDefault(s => s.Id == request.ServiceId);
        if (service is null)
            return Error.NotFoundFor("Service", request.ServiceId);

        ServiceManifestDto manifest;
        try
        {
            manifest = await manifestParser.ParseAsync(request.ManifestYaml, cancellationToken);
        }
        catch (Exception ex)
        {
            return new Error("General.Validation", $"Invalid manifest YAML: {ex.Message}");
        }

        if (!string.Equals(manifest.Name, service.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameConflict = environment.Services.Any(s =>
                s.Id != request.ServiceId &&
                string.Equals(s.Name, manifest.Name, StringComparison.OrdinalIgnoreCase));
            if (nameConflict)
                return Error.ConflictFor("Service", manifest.Name);
        }

        var serviceData = manifest.ToServiceData();
        environment.UpdateService(request.ServiceId, manifest.Name, manifest.Type, manifest.ExposureMode, manifest.Alias, serviceData.SourceConfig);

        return Result.Success();
    }
}
