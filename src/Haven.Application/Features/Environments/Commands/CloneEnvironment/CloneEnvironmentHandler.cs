using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Entities.Environment;
using EnvironmentVariable = Haven.Domain.Entities.EnvironmentVariables;
using FeatureFlag = Haven.Domain.Entities.FeatureFlag;

namespace Haven.Application.Features.Environments.Commands.CloneEnvironment;

public sealed class CloneEnvironmentHandler(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    INetworkRepository networkRepository,
    IServiceRepository serviceRepository,
    IEnvironmentVariableRepository envVarRepository,
    IFeatureFlagRepository featureFlagRepository)
    : Common.Messaging.ICommandHandler<CloneEnvironmentCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CloneEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var sourceProject = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (sourceProject is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var sourceEnvironment = sourceProject.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (sourceEnvironment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var targetProjectId = request.TargetProjectId ?? request.ProjectId;
        Project targetProject;

        if (targetProjectId == request.ProjectId)
        {
            targetProject = sourceProject;
        }
        else
        {
            var found = await projectRepository.GetByIdAsync(targetProjectId, cancellationToken);
            if (found is null)
                return Error.NotFoundFor(nameof(Project), targetProjectId);
            targetProject = found;
        }

        if (targetProject.Environments.Any(e => string.Equals(e.Name, request.NewName, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Environment", request.NewName);

        var clonedEnvironment = targetProject.AddEnvironment(request.NewName, request.NewAlias, sourceEnvironment.Description);
        environmentRepository.AddAsync(clonedEnvironment, cancellationToken: cancellationToken);

        var network = Network.CreateProjectEnvironmentNetwork(
            targetProject.Id,
            targetProject.Alias ?? targetProject.Id.ToString("N")[..8],
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
            var clonedService = targetProject.AddService(
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

        return Result<Guid>.CreatedFor(clonedEnvironment.Id);
    }
}