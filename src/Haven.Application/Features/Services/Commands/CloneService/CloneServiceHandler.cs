using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using Environment = Haven.Domain.Entities.Environment;
using EnvironmentVariable = Haven.Domain.Entities.EnvironmentVariables;
using FeatureFlag = Haven.Domain.Entities.FeatureFlag;
using Service = Haven.Domain.Entities.Service;

namespace Haven.Application.Features.Services.Commands.CloneService;

public sealed class CloneServiceHandler(
    IProjectRepository projectRepository,
    IServiceRepository serviceRepository,
    IEnvironmentVariableRepository envVarRepository,
    IFeatureFlagRepository featureFlagRepository)
    : Common.Messaging.ICommandHandler<CloneServiceCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CloneServiceCommand request, CancellationToken cancellationToken)
    {
        var sourceProject = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (sourceProject is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var sourceEnvironment = sourceProject.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (sourceEnvironment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var sourceService = sourceEnvironment.Services.FirstOrDefault(s => s.Id == request.ServiceId);
        if (sourceService is null)
            return Error.NotFoundFor(nameof(Service), request.ServiceId);

        var targetProjectId = request.TargetProjectId ?? request.ProjectId;
        var targetEnvironmentId = request.TargetEnvironmentId ?? request.EnvironmentId;

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

        Environment targetEnvironment;
        if (targetEnvironmentId == request.EnvironmentId && targetProjectId == request.ProjectId)
        {
            targetEnvironment = sourceEnvironment;
        }
        else
        {
            var found = targetProject.Environments.FirstOrDefault(e => e.Id == targetEnvironmentId);
            if (found is null)
                return Error.NotFoundFor(nameof(Environment), targetEnvironmentId);
            targetEnvironment = found;
        }

        if (targetEnvironment.Services.Any(s => string.Equals(s.Name, request.NewName, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Service", request.NewName);

        var clonedService = targetProject.AddService(
            targetEnvironment.Id,
            request.NewName,
            sourceService.Type,
            sourceService.ExposureMode,
            request.NewAlias ?? sourceService.Alias,
            sourceService.SourceConfig);

        await serviceRepository.AddAsync(clonedService, cancellationToken);

        var envVars = await envVarRepository.GetForServiceAsync(sourceService.Id, cancellationToken);
        var clonedEnvVars = envVars.Select(v => new EnvironmentVariable
        {
            ParentId = clonedService.Id,
            ParentType = v.ParentType,
            Key = v.Key,
            Value = v.Value
        }).ToList();

        if (clonedEnvVars.Count > 0)
            await envVarRepository.AddAsync(clonedEnvVars, cancellationToken);

        var featureFlags = await featureFlagRepository.GetForServiceListAsync(sourceService.Id, cancellationToken);
        var clonedFlags = featureFlags.Select(f => FeatureFlag.Create(
            clonedService.Id, f.Name, f.Type, f.Key, f.Description, f.Value, f.ValueType)).ToList();

        if (clonedFlags.Count > 0)
            await featureFlagRepository.AddAsync(clonedFlags, cancellationToken);

        return Result<Guid>.CreatedFor(clonedService.Id);
    }
}
