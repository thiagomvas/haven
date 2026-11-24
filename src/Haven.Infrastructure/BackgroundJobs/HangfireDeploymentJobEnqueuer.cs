using Hangfire;
using Hangfire.States;

using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HangfireDeploymentJobEnqueuer(
    IBackgroundJobClient backgroundJobClient,
    IDeploymentRepository deploymentRepository,
    ILogger<HangfireDeploymentJobEnqueuer> logger)
    : IDeploymentJobEnqueuer
{
    private const string DeploymentQueueName = "deployments";

    public Task EnqueueDeployment(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default)
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Deploy, ct);

    public Task EnqueueStart(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default)
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Start, ct);

    public Task EnqueueStop(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default)
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Stop, ct);

    public Task EnqueueRestart(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default)
        => Enqueue(projectId, environmentId, serviceId, ServiceJobOperation.Restart, ct);

    public Task EnqueueSidecarDeployment(Guid sidecarId, CancellationToken ct = default)
        => EnqueueSidecar(sidecarId, ServiceJobOperation.Deploy);

    public Task EnqueueSidecarStop(Guid sidecarId, CancellationToken ct = default)
        => EnqueueSidecar(sidecarId, ServiceJobOperation.Stop);

    private async Task Enqueue(Guid projectId, Guid environmentId, Guid serviceId, ServiceJobOperation operation, CancellationToken ct)
    {
        if (operation == ServiceJobOperation.Deploy && await HasPendingDeploymentAsync(serviceId, ct))
        {
            logger.LogInformation(
                "Skipped enqueueing {Operation} for service {ServiceId}: a deployment is already pending",
                operation, serviceId);
            return;
        }

        var jobId = backgroundJobClient.Create<DeploymentBackgroundJob>(
            x => x.ExecuteOperationAsync(projectId, environmentId, serviceId, operation),
            new EnqueuedState(DeploymentQueueName));

        logger.LogInformation(
            "Enqueued {Operation} for project {ProjectId}, environment {EnvironmentId}, service {ServiceId} (Job ID: {JobId})",
            operation, projectId, environmentId, serviceId, jobId);
    }

    private Task EnqueueSidecar(Guid sidecarId, ServiceJobOperation operation)
    {
        var jobId = backgroundJobClient.Create<SidecarDeploymentBackgroundJob>(
            x => x.ExecuteOperationAsync(sidecarId, operation),
            new EnqueuedState(DeploymentQueueName));

        logger.LogInformation(
            "Enqueued {Operation} for sidecar {SidecarId} (Job ID: {JobId})",
            operation, sidecarId, jobId);

        return Task.CompletedTask;
    }

    private async Task<bool> HasPendingDeploymentAsync(Guid serviceId, CancellationToken ct)
    {
        var active = await deploymentRepository.FindActiveForServiceAsync(serviceId, ct);

        return active is { Status: DeploymentStatus.Queued };
    }
}
