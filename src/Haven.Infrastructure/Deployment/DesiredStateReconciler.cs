using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment;

/// <summary>
/// Compares each service's and sidecar's desired state with the runtime state on its registry entry and
/// starts or stops the container when they have drifted apart.
/// </summary>
public sealed class DesiredStateReconciler(
    HavenDbContext db,
    IDeploymentRepository deploymentRepository,
    IDeploymentOrchestrator orchestrator,
    ILogger<DesiredStateReconciler> logger)
{
    public async Task ReconcileAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var entries = await db.ServiceRegistryEntries.ToListAsync(ct);
        var entriesByOwner = entries.ToDictionary(e => e.OwnerId);

        var services = await db.Services
            .Include(s => s.Environment!)
            .ThenInclude(e => e.Project)
            .ToListAsync(ct);
        var activeDeployments = await deploymentRepository.GetActiveStatusesAsync(services.Select(s => s.Id).ToList(), ct);

        foreach (var service in services)
            await ReconcileAsync(service, entriesByOwner, activeDeployments.ContainsKey(service.Id), now, ct);

        var sidecars = await db.Sidecars.ToListAsync(ct);
        foreach (var sidecar in sidecars)
            await ReconcileAsync(sidecar, entriesByOwner, deploymentActive: false, now, ct);
    }

    private async Task ReconcileAsync(
        IDeployableContainer container,
        Dictionary<Guid, ServiceRegistryEntry> entriesByOwner,
        bool deploymentActive,
        DateTime now,
        CancellationToken ct)
    {
        // A missing entry means the container has never been deployed.
        entriesByOwner.TryGetValue(container.Id, out var entry);
        var runtime = entry?.RuntimeState ?? ServiceRuntimeState.NotDeployed;
        var changedAt = entry?.StateChangedAt ?? DateTime.MinValue;

        var action = DesiredStateDrift.Decide(
            container.DesiredState, runtime, deploymentActive, changedAt, now,
            (container.SourceConfig as DockerBasedSourceConfig)?.RestartPolicy ?? RestartPolicy.UnlessStopped);
        if (action == DriftAction.None) return;

        logger.LogWarning(
            "{Name} ({Id}) wants to be {Desired} but is {Runtime}. Reconciling with {Action}.",
            container.Name, container.Id, container.DesiredState, runtime, action);

        try
        {
            var result = action == DriftAction.Start
                ? await orchestrator.StartAsync(container, ct)
                : await orchestrator.StopAsync(container, ct);

            if (result.IsFailure)
                logger.LogWarning("Reconciling {Name} ({Id}) with {Action} failed: {Error}", container.Name, container.Id, action, result.Error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reconciling {Name} ({Id}) with {Action} threw", container.Name, container.Id, action);
        }
    }
}