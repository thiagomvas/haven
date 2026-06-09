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
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Deploy);

    public void EnqueueStart(Guid projectId, Guid environmentId, Guid serviceId)
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Start);

    public void EnqueueStop(Guid projectId, Guid environmentId, Guid serviceId)
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Stop);

    public void EnqueueRestart(Guid projectId, Guid environmentId, Guid serviceId)
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Restart);

    private void Enqueue(Guid projectId, Guid environmentId, Guid serviceId, ServiceJobOperation operation)
    {
        var jobId = backgroundJobClient.Create<DeploymentBackgroundJob>(
            x => x.ExecuteOperationAsync(projectId, environmentId, serviceId, operation),
            new EnqueuedState(DeploymentQueueName));

        logger.LogInformation(
            "Enqueued {Operation} for project {ProjectId}, environment {EnvironmentId}, service {ServiceId} (Job ID: {JobId})",
            operation, projectId, environmentId, serviceId, jobId);
    }
}