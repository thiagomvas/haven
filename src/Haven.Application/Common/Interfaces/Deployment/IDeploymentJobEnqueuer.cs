namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentJobEnqueuer
{
    void EnqueueDeployment(Guid projectId, Guid environmentId, Guid serviceId);
    void EnqueueStart(Guid projectId, Guid environmentId, Guid serviceId);
    void EnqueueStop(Guid projectId, Guid environmentId, Guid serviceId);
    void EnqueueRestart(Guid projectId, Guid environmentId, Guid serviceId);
}
