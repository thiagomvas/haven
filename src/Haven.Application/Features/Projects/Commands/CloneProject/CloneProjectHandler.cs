using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using EnvironmentVariable = Haven.Domain.Entities.EnvironmentVariables;
using FeatureFlag = Haven.Domain.Entities.FeatureFlag;

namespace Haven.Application.Features.Projects.Commands.CloneProject;

public sealed class CloneProjectHandler(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    INetworkRepository networkRepository,
    IServiceRepository serviceRepository,
    IEnvironmentVariableRepository envVarRepository,
    IFeatureFlagRepository featureFlagRepository)
    : Common.Messaging.ICommandHandler<CloneProjectCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CloneProjectCommand request, CancellationToken cancellationToken)
    {
        var sourceProject = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (sourceProject is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var nameExists = await projectRepository.ExistsWithNameAsync(request.NewName, Guid.Empty, cancellationToken);
        if (nameExists)
            return Error.ConflictFor(nameof(Project), request.NewName);

        var clonedProject = Project.Create(request.NewName, request.NewAlias ?? sourceProject.Alias, sourceProject.Description);
        await projectRepository.AddAsync(clonedProject, cancellationToken);

        var projectEnvVars = await envVarRepository.GetForProjectAsync(sourceProject.Id, cancellationToken);
        var clonedProjectEnvVars = projectEnvVars.Select(v => new EnvironmentVariable
        {
            ParentId = clonedProject.Id,
            ParentType = v.ParentType,
            Key = v.Key,
            Value = v.Value
        }).ToList();

        if (clonedProjectEnvVars.Count > 0)
            await envVarRepository.AddAsync(clonedProjectEnvVars, cancellationToken);

        foreach (var sourceEnvironment in sourceProject.Environments)
        {
            var clonedEnvironment = clonedProject.AddEnvironment(
                sourceEnvironment.Name,
                sourceEnvironment.Alias,
                sourceEnvironment.Description);

            environmentRepository.AddAsync(clonedEnvironment, cancellationToken: cancellationToken);

            var network = Network.CreateProjectEnvironmentNetwork(
                clonedProject.Id,
                clonedProject.Alias ?? clonedProject.Id.ToString("N")[..8],
                clonedEnvironment.Id,
                clonedEnvironment.Alias ?? clonedEnvironment.Id.ToString("N")[..8]);
            await networkRepository.AddAsync(network, cancellationToken);

            var envEnvVars = await envVarRepository.GetForEnvironmentAsync(sourceEnvironment.Id, cancellationToken);
            var clonedEnvEnvVars = envEnvVars.Select(v => new EnvironmentVariable
            {
                ParentId = clonedEnvironment.Id,
                ParentType = v.ParentType,
                Key = v.Key,
                Value = v.Value
            }).ToList();

            if (clonedEnvEnvVars.Count > 0)
                await envVarRepository.AddAsync(clonedEnvEnvVars, cancellationToken);

            foreach (var sourceService in sourceEnvironment.Services)
            {
                var clonedService = clonedProject.AddService(
                    clonedEnvironment.Id,
                    sourceService.Name,
                    sourceService.Type,
                    sourceService.ExposureMode,
                    sourceService.Alias,
                    sourceService.SourceConfig);

                await serviceRepository.AddAsync(clonedService, cancellationToken);

                var serviceEnvVars = await envVarRepository.GetForServiceAsync(sourceService.Id, cancellationToken);
                var clonedServiceEnvVars = serviceEnvVars.Select(v => new EnvironmentVariable
                {
                    ParentId = clonedService.Id,
                    ParentType = v.ParentType,
                    Key = v.Key,
                    Value = v.Value
                }).ToList();

                if (clonedServiceEnvVars.Count > 0)
                    await envVarRepository.AddAsync(clonedServiceEnvVars, cancellationToken);

                var featureFlags = await featureFlagRepository.GetForServiceListAsync(sourceService.Id, cancellationToken);
                var clonedFlags = featureFlags.Select(f => FeatureFlag.Create(
                    clonedService.Id, f.Name, f.Type, f.Key, f.Description, f.Value, f.ValueType)).ToList();

                if (clonedFlags.Count > 0)
                    await featureFlagRepository.AddAsync(clonedFlags, cancellationToken);
            }
        }

        return Result<Guid>.CreatedFor(clonedProject.Id);
    }
}
