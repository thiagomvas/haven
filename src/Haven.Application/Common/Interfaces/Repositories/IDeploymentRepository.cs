namespace Haven.Application.Common.Interfaces.Repositories;

public interface IDeploymentRepository : IRepository
{
    Task<Domain.Entities.Deployment> FindByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Domain.Entities.Deployment deployment, CancellationToken ct);
    Task<List<Domain.Entities.Deployment>> GetAllForServiceAsync(Guid serviceId, CancellationToken ct);
    /// <summary>Status of the first non-terminal deployment per service; services without one are absent.</summary>
    Task<Dictionary<Guid, Haven.Domain.Enums.DeploymentStatus>> GetActiveStatusesAsync(IReadOnlyCollection<Guid> serviceIds, CancellationToken ct);
    /// <summary>Services whose most recent deployment finished in a failed state.</summary>
    Task<HashSet<Guid>> GetServicesWithLatestFailedDeploymentAsync(IReadOnlyCollection<Guid> serviceIds, CancellationToken ct);
    /// <summary>Most recent deployments across all services, newest first, with the service, environment and project loaded.</summary>
    Task<List<Domain.Entities.Deployment>> GetRecentAsync(int limit, CancellationToken ct);
    Task<Domain.Entities.Deployment?> FindActiveForServiceAsync(Guid serviceId, CancellationToken ct);
    Task RemoveAsync(Guid deploymentId, CancellationToken ct);
    Task<List<Domain.Entities.Deployment>> GetExcessDeploymentsAsync(int retentionCount, CancellationToken ct);
    Task<List<Guid>> FilterMissingIdsAsync(List<Guid> ids, CancellationToken ct);
}