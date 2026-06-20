using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class DeploymentBackgroundJob(
    IProjectRepository projectRepository,
    IDeploymentOrchestrator orchestrator,
    IDeploymentCancellationService cancellationService,
    IUnitOfWork unitOfWork,
    ILogger<DeploymentBackgroundJob> logger)
{
    public async Task<Result> ExecuteAsync(Guid projectId, Guid environmentId, Guid serviceId)
        => await ExecuteOperationAsync(projectId, environmentId, serviceId, ServiceJobOperation.Deploy);

    public async Task<Result> ExecuteOperationAsync(
        Guid projectId,
        Guid environmentId,
        Guid serviceId,
        ServiceJobOperation operation)
    {
        logger.LogInformation(
            "Starting {Operation} for project {ProjectId}, environment {EnvironmentId}, service {ServiceId}",
            operation, projectId, environmentId, serviceId);

        var project = await projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        if (project is null)
        {
            logger.LogError("Project {ProjectId} not found during {Operation} execution", projectId, operation);
            return Result.Failure(Error.NotFoundFor("Project", projectId));
        }

        var environment = project.Environments.FirstOrDefault(e => e.Id == environmentId);
        if (environment is null)
        {
            logger.LogError(
                "Environment {EnvironmentId} not found in project {ProjectId}",
                environmentId, projectId);
            return Result.Failure(Error.NotFoundFor("Environment", environmentId));
        }

        var service = environment.Services.FirstOrDefault(s => s.Id == serviceId);
        if (service is null)
        {
            logger.LogError(
                "Service {ServiceId} not found in environment {EnvironmentId}",
                serviceId, environmentId);
            return Result.Failure(Error.NotFoundFor("Service", serviceId));
        }

        logger.LogInformation(
            "Executing {Operation} on service {ServiceName} ({ServiceId}) in environment {EnvironmentName}",
            operation, service.Name, serviceId, environment.Name);

        var ct = operation == ServiceJobOperation.Deploy
            ? cancellationService.Register(serviceId)
            : CancellationToken.None;

        Result result;
        try
        {
            result = operation switch
            {
                ServiceJobOperation.Deploy => await orchestrator.DeployServiceAsync(service, ct),
                ServiceJobOperation.Start => await orchestrator.StartServiceAsync(service, CancellationToken.None),
                ServiceJobOperation.Stop => await orchestrator.StopServiceAsync(service, CancellationToken.None),
                ServiceJobOperation.Restart => await orchestrator.RestartServiceAsync(service, CancellationToken.None),
                _ => Result.Failure(Error.Failure("Deploy.UnknownOperation", $"Unknown operation: {operation}"))
            };
        }
        finally
        {
            if (operation == ServiceJobOperation.Deploy)
                cancellationService.Unregister(serviceId);
        }

        if (result.IsSuccess)
            logger.LogInformation("{Operation} succeeded for service {ServiceId}", operation, serviceId);
        else
            logger.LogError("{Operation} failed for service {ServiceId}: {Error}", operation, serviceId, result.Error);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return result;
    }
}