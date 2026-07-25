using Hangfire;
using Hangfire.States;

using Haven.Application.Common.Interfaces.Deployment;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HangfireServiceCleanupJobEnqueuer(
    IBackgroundJobClient backgroundJobClient,
    ILogger<HangfireServiceCleanupJobEnqueuer> logger)
    : IServiceCleanupJobEnqueuer
{
    private const string DeploymentQueueName = "deployments";

    public void EnqueueCleanup(ServiceCleanupInfo info)
    {
        var jobId = backgroundJobClient.Create<ServiceCleanupBackgroundJob>(
            x => x.ExecuteAsync(info),
            new EnqueuedState(DeploymentQueueName));

        logger.LogInformation(
            "Enqueued deployment cleanup for removed service {ServiceId} ({ServiceName}) (Job ID: {JobId})",
            info.ServiceId, info.ServiceName, jobId);
    }
}
