using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Application.Features.Exporting;
using Haven.Application.Mappers;

using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Environments.Commands.ExportEnvironmentToDockerCompose;

public sealed class ExportEnvironmentToDockerComposeHandler(
    IEnvironmentRepository environmentRepository,
    IServiceRepository serviceRepository,
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagService featureFlagService,
    IExportFormatFactory exportFormatFactory,
    IOptionsMonitor<VolumesOptions> volumesOptions)
    : Common.Messaging.ICommandHandler<ExportEnvironmentToDockerComposeCommand, string>
{
    public async ValueTask<Result<string>> Handle(ExportEnvironmentToDockerComposeCommand request, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var environmentServices = await serviceRepository.GetByEnvironmentIdAsync(request.EnvironmentId, cancellationToken);
        var selectedServices = environmentServices.Where(s => request.ServiceIds.Contains(s.Id)).ToList();

        var exportModels = new List<ServiceExportModel>(selectedServices.Count);
        foreach (var service in selectedServices)
        {
            var environmentVariables = await environmentVariableService.BuildVariablesForServiceAsync(service.Id, cancellationToken);
            var featureFlags = await featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(service.Id, cancellationToken);
            environmentVariables.AddRange(featureFlags);

            exportModels.Add(service.ToExportModel(environmentVariables, volumesOptions.CurrentValue.RootPath));
        }

        var exportFormat = exportFormatFactory.Create(ExportFormatType.DockerCompose);
        var composeFile = exportFormat.ExportEnvironment(exportModels);

        return Result<string>.Success(composeFile);
    }
}