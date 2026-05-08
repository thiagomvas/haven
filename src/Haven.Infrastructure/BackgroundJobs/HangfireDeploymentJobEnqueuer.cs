using Hangfire;
using Hangfire.States;
using Haven.Application.Common.Interfaces.Deployment;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HangfireDeploymentJobEnqueuer(
    IBackgroundJobClient backgroundJobClient,
    ILogger<HangfireDeploymentJobEnqueuer> logger)
    : IDeploymentJobEnqueuer
{
    private const string DeploymentQueueName = "deployments";

    public void EnqueueDeployment(Guid projectId, Guid environmentId, Guid serviceId)
    {
        var jobId = backgroundJobClient.Create<DeploymentBackgroundJob>(
            x => x.ExecuteAsync(projectId, environmentId, serviceId),
            new EnqueuedState(DeploymentQueueName));

        logger.LogInformation(
            "Enqueued deployment for project {ProjectId}, environment {EnvironmentId}, service {ServiceId} (Job ID: {JobId})",
            projectId, environmentId, serviceId, jobId);
    }
}
