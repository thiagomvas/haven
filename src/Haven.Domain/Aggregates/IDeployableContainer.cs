using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Aggregates;

/// <summary>
/// Common surface shared by <see cref="Service"/> and <see cref="Sidecar"/> so the deployment
/// orchestrator, reconciliation, and monitoring jobs can operate on either kind of container
/// without duplicating lifecycle logic.
/// </summary>
public interface IDeployableContainer
{
    Guid Id { get; }
    string Name { get; }
    string? Alias { get; }
    ServiceStatus Status { get; }
    ServiceHealth Health { get; }
    ServiceSourceConfig? SourceConfig { get; }
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    DateTime? LastDeployedAt { get; }

    void MarkDeploymentPending();
    void MarkDeploying();
    void MarkDeployed();
    void MarkStopped();
}