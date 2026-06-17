namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentLogService
{
    Task<Domain.Entities.Deployment> CreateDeploymentForServiceAsync(Guid serviceId, CancellationToken ct);
    Task AppendLogAsync(Guid deploymentId, string logEntry, CancellationToken ct);
    Task MarkDeploymentCompletedAsync(Guid deploymentId, CancellationToken ct);
    Task MarkDeploymentFailedAsync(Guid deploymentId, CancellationToken ct);
}