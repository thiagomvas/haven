using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class DeploymentBackgroundJob(
    IProjectRepository projectRepository,
    IDeployServiceFactory deployServiceFactory,
    IUnitOfWork unitOfWork,
    ILogger<DeploymentBackgroundJob> logger)
{
    public async Task ExecuteAsync(Guid projectId, Guid environmentId, Guid serviceId)
    {
        logger.LogInformation(
            "Starting deployment for project {ProjectId}, environment {EnvironmentId}, service {ServiceId}",
            projectId, environmentId, serviceId);

        var project = await projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        if (project is null)
        {
            logger.LogError(
                "Project {ProjectId} not found during deployment execution",
                projectId);
            return;
        }

        var environment = project.Environments.FirstOrDefault(e => e.Id == environmentId);
        if (environment is null)
        {
            logger.LogError(
                "Environment {EnvironmentId} not found in project {ProjectId}",
                environmentId, projectId);
            return;
        }

        var service = environment.Services.FirstOrDefault(s => s.Id == serviceId);
        if (service is null)
        {
            logger.LogError(
                "Service {ServiceId} not found in environment {EnvironmentId}",
                serviceId, environmentId);
            return;
        }

        logger.LogInformation(
            "Deploying service {ServiceName} ({ServiceId}) to environment {EnvironmentName}",
            service.Name, serviceId, environment.Name);

        var deployService = deployServiceFactory.Create(service);
        var deployResult = await deployService.DeployAsync(service, CancellationToken.None);

        if (deployResult.IsSuccess)
        {
            project.DeployService(environmentId, serviceId);
            logger.LogInformation(
                "Deployment succeeded for service {ServiceId}",
                serviceId);
        }
        else
        {
            logger.LogError(
                "Deployment failed for service {ServiceId}: {Error}",
                serviceId, deployResult.Error);
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}
