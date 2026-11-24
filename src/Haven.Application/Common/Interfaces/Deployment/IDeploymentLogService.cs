using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentLogService
{
    Task<Domain.Entities.Deployment> CreateDeploymentForServiceAsync(Guid serviceId, CancellationToken ct);
    /// <summary>
    /// Records a <c>Queued</c> deployment as soon as one is requested, so the UI can show it before a worker
    /// picks it up. If the service already has an in-flight deployment it is returned with
    /// <see cref="QueuedDeployment.Created"/> false, and the caller must not enqueue another job for it.
    /// </summary>
    Task<QueuedDeployment> QueueDeploymentForServiceAsync(Guid serviceId, CancellationToken ct);
    Task MarkDeploymentStartingAsync(Guid deploymentId, CancellationToken ct);
    Task SetIdentifierAsync(Guid deploymentId, DeploymentIdentifierType type, string identifier, string? commitMessage, CancellationToken ct);
    Task AppendLogAsync(Guid deploymentId, string logEntry, CancellationToken ct);
    Task MarkDeploymentCompletedAsync(Guid deploymentId, CancellationToken ct);
    Task MarkDeploymentFailedAsync(Guid deploymentId, CancellationToken ct);
    Task MarkDeploymentCancelledAsync(Guid deploymentId, CancellationToken ct);
}

public sealed record QueuedDeployment(Domain.Entities.Deployment Deployment, bool Created);
