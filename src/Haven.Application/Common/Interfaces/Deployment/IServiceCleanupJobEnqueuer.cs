namespace Haven.Application.Common.Interfaces.Deployment;

public interface IServiceCleanupJobEnqueuer
{
    void EnqueueCleanup(ServiceCleanupInfo info);
}