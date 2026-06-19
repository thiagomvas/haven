namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentCancellationService
{
    CancellationToken Register(Guid serviceId);
    void Cancel(Guid serviceId);
    void Unregister(Guid serviceId);
    bool IsRegistered(Guid serviceId);
}
