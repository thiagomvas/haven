namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentJobEnqueuer
{
    Task EnqueueDeployment(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default);
    Task EnqueueStart(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default);
    Task EnqueueStop(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default);
    Task EnqueueRestart(Guid projectId, Guid environmentId, Guid serviceId, CancellationToken ct = default);

    Task EnqueueSidecarDeployment(Guid sidecarId, CancellationToken ct = default);
    Task EnqueueSidecarStop(Guid sidecarId, CancellationToken ct = default);
}