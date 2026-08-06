namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Backfills/refreshes Docker-derived network data (subnet, gateway, per-service IP addresses)
/// that Haven doesn't always capture at the moment it happens (e.g. legacy rows created before
/// this data was tracked, or drift from manual Docker changes).
/// </summary>
public interface INetworkReconciliationService
{
    Task ReconcileAsync(CancellationToken cancellationToken);
}
