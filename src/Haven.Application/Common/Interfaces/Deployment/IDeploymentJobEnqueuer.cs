namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentJobEnqueuer
{
    void EnqueueDeployment(Guid projectId, Guid environmentId, Guid serviceId);
}
