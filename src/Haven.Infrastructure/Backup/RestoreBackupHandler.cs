using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Converters;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Aggregates.Environment;
using Service = Haven.Domain.Aggregates.Service;

namespace Haven.Infrastructure.Backup;

public sealed class RestoreBackupHandler(
    IBackupManifestReader manifestReader,
    IManifestSerializer<Project> projectSerializer,
    IManifestSerializer<Environment> environmentSerializer,
    IManifestSerializer<Network> networkSerializer,
    IManifestSerializer<Sidecar> sidecarSerializer,
    IManifestSerializer<Service> serviceSerializer,
    HavenDbContext context,
    IBackupManifestWriter manifestWriter,
    IOptionsMonitor<ManifestsOptions> manifestsOptions,
    IOptionsMonitor<VolumesOptions> volumesOptions,
    IBackupCoordinationLock coordinationLock,
    IServiceCleanupJobEnqueuer serviceCleanupJobEnqueuer,
    IEncryptionService encryptionService,
    ILogger<RestoreBackupHandler> logger)
    : ICommandHandler<RestoreBackupCommand, RestoreBackupResult>
{
    public async ValueTask<Result<RestoreBackupResult>> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        IDisposable? release = null;
        if (!request.DryRun && !coordinationLock.TryAcquire(out release))
            return Result<RestoreBackupResult>.Failure(Error.BackupOperationInProgress);

        string? tempDir = null;
        try
        {
            var sourceDir = await manifestReader.PrepareSourceDirectoryAsync(
                request.Source, request.SnapshotName, request.CommitSha, ct);

            if (request.Source == RestoreSource.Git)
                tempDir = sourceDir; // only Git extracts to a temp dir that needs cleanup

            var snapshotProjects = await projectSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotProjectById = snapshotProjects.ToDictionary(p => p.Id);

            var snapshotEnvironments = await environmentSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotEnvironmentById = snapshotEnvironments.ToDictionary(e => e.Id);

            var snapshotNetworks = await networkSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotNetworkById = snapshotNetworks.ToDictionary(n => n.Id);

            var snapshotSidecars = await sidecarSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotSidecarByKind = snapshotSidecars.ToDictionary(s => s.Kind);

            var snapshotServices = await serviceSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotServiceById = snapshotServices.ToDictionary(s => s.Id);

            var currentProjects = await context.Projects.AsNoTracking().ToListAsync(ct);
            var currentProjectById = currentProjects.ToDictionary(p => p.Id);

            var currentEnvironments = await context.Environments.AsNoTracking().ToListAsync(ct);
            var currentEnvironmentById = currentEnvironments.ToDictionary(e => e.Id);

            // BackupManifestWriter backs up every network except System (the single auto-regenerated
            // control-plane network) - the "current" side of the diff must be scoped the same way,
            // otherwise the excluded System network (e.g. the built-in "haven-system" sidecar
            // network) would show up as deleted on every restore, and a non-dry-run restore would
            // actually delete it.
            var currentNetworks = await context.Networks
                .Where(n => n.Type != NetworkType.System)
                .Include(n => n.ServiceNetworks)
                .AsNoTracking()
                .ToListAsync(ct);
            var currentNetworkById = currentNetworks.ToDictionary(n => n.Id);

            var currentSidecars = await context.Sidecars.AsNoTracking().ToListAsync(ct);
            var currentSidecarByKind = currentSidecars.ToDictionary(s => s.Kind);

            var currentServices = await context.Services.Include(s => s.Volumes).AsNoTracking().ToListAsync(ct);
            var currentServiceById = currentServices.ToDictionary(s => s.Id);

            var snapshotEnvVars = await ReadSnapshotEnvVarsAsync(
                sourceDir, snapshotProjectById, snapshotEnvironmentById, snapshotServiceById, ct);

            var currentEnvVars = await context.EnvironmentVariables.AsNoTracking().ToListAsync(ct);

            var projectsDiff = ComputeProjectDiff(snapshotProjects, snapshotProjectById, currentProjectById);
            var environmentsDiff = ComputeEnvironmentDiff(snapshotEnvironments, snapshotEnvironmentById, currentEnvironmentById, snapshotProjectById, currentProjectById);
            var networksDiff = ComputeNetworkDiff(snapshotNetworks, snapshotNetworkById, currentNetworkById);
            var sidecarsDiff = ComputeSidecarDiff(snapshotSidecars, snapshotSidecarByKind, currentSidecarByKind);
            var volumeFilesDiff = ComputeVolumeFileDiff(
                sourceDir, snapshotProjectById, snapshotEnvironmentById, snapshotServiceById);
            var servicesWithVolumeFileChanges = volumeFilesDiff.Created
                .Concat(volumeFilesDiff.Updated)
                .Concat(volumeFilesDiff.Deleted)
                .Select(f => f.ServiceId)
                .ToHashSet();
            var servicesDiff = ComputeServiceDiff(snapshotServices, snapshotServiceById, currentServiceById, snapshotEnvironmentById, currentEnvironmentById, snapshotProjectById, currentProjectById, servicesWithVolumeFileChanges);
            var envVarsDiff = ComputeEnvVarDiff(
                snapshotEnvVars, currentEnvVars,
                snapshotProjectById, currentProjectById,
                snapshotEnvironmentById, currentEnvironmentById,
                snapshotServiceById, currentServiceById);

            List<string> volumeFileRestoreWarnings = [];
            if (!request.DryRun)
            {
                await ApplyChangesAsync(
                    snapshotProjects, snapshotProjectById, currentProjectById,
                    snapshotEnvironments, snapshotEnvironmentById, currentEnvironmentById,
                    snapshotNetworks, snapshotNetworkById, currentNetworkById,
                    snapshotSidecars, snapshotSidecarByKind, currentSidecarByKind,
                    snapshotServices, snapshotServiceById, currentServiceById,
                    snapshotEnvVars, snapshotProjectById.Keys, snapshotEnvironmentById.Keys, snapshotServiceById.Keys,
                    ct);

                volumeFileRestoreWarnings = RestoreVolumeFiles(
                    sourceDir, snapshotProjectById, snapshotEnvironmentById, snapshotServiceById, currentServiceById);

                await manifestWriter.WriteAllAsync(manifestsOptions.CurrentValue.ManifestsPath, ct);
            }

            logger.LogInformation(
                "Restore (DryRun={DryRun}): projects +{PC}~{PU}-{PD}, environments +{EC}~{EU}-{ED}, networks +{NC}~{NU}-{ND}, sidecars +{SiC}~{SiU}-{SiD}, services +{SC}~{SU}-{SD}, envVars +{VC}~{VU}-{VD}",
                request.DryRun,
                projectsDiff.Created.Count, projectsDiff.Updated.Count, projectsDiff.Deleted.Count,
                environmentsDiff.Created.Count, environmentsDiff.Updated.Count, environmentsDiff.Deleted.Count,
                networksDiff.Created.Count, networksDiff.Updated.Count, networksDiff.Deleted.Count,
                sidecarsDiff.Created.Count, sidecarsDiff.Updated.Count, sidecarsDiff.Deleted.Count,
                servicesDiff.Created.Count, servicesDiff.Updated.Count, servicesDiff.Deleted.Count,
                envVarsDiff.Created.Count, envVarsDiff.Updated.Count, envVarsDiff.Deleted.Count);

            return Result<RestoreBackupResult>.Success(new RestoreBackupResult
            {
                DryRun = request.DryRun,
                Projects = projectsDiff,
                Environments = environmentsDiff,
                Networks = networksDiff,
                Sidecars = sidecarsDiff,
                Services = servicesDiff,
                EnvironmentVariables = envVarsDiff,
                VolumeFiles = volumeFilesDiff,
                VolumeFileRestoreWarnings = volumeFileRestoreWarnings
            });
        }
        finally
        {
            if (tempDir is not null && Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to clean up temporary restore directory {Dir}", tempDir);
                }
            }

            release?.Dispose();
        }
    }

    private static EntityChangeSummary<ProjectRestoreItem> ComputeProjectDiff(
        IReadOnlyList<Project> snapshot,
        Dictionary<Guid, Project> snapshotById,
        Dictionary<Guid, Project> currentById)
    {
        var (created, updated, deleted) = ManifestDiffEngine.Compute(
            snapshot, currentById.Values.ToList(), p => p.Id, HasProjectChanges);

        return new EntityChangeSummary<ProjectRestoreItem>
        {
            Created = created.Select(p => new ProjectRestoreItem(p.Id, p.Name)).ToList(),
            Updated = updated.Select(p => new ProjectRestoreItem(p.Id, p.Name)).ToList(),
            Deleted = deleted.Select(p => new ProjectRestoreItem(p.Id, p.Name)).ToList()
        };
    }

    private static EntityChangeSummary<EnvironmentRestoreItem> ComputeEnvironmentDiff(
        IReadOnlyList<Environment> snapshot,
        Dictionary<Guid, Environment> snapshotById,
        Dictionary<Guid, Environment> currentById,
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Project> currentProjectById)
    {
        var (created, updated, deleted) = ManifestDiffEngine.Compute(
            snapshot, currentById.Values.ToList(), e => e.Id, HasEnvironmentChanges);

        EnvironmentRestoreItem ToSnapshotItem(Environment e) =>
            new(e.Id, e.Name, e.ProjectId, snapshotProjectById.GetValueOrDefault(e.ProjectId)?.Name);
        EnvironmentRestoreItem ToCurrentItem(Environment e) =>
            new(e.Id, e.Name, e.ProjectId, currentProjectById.GetValueOrDefault(e.ProjectId)?.Name);

        return new EntityChangeSummary<EnvironmentRestoreItem>
        {
            Created = created.Select(ToSnapshotItem).ToList(),
            Updated = updated.Select(ToSnapshotItem).ToList(),
            Deleted = deleted.Select(ToCurrentItem).ToList()
        };
    }

    private static EntityChangeSummary<NetworkRestoreItem> ComputeNetworkDiff(
        IReadOnlyList<Network> snapshot,
        Dictionary<Guid, Network> snapshotById,
        Dictionary<Guid, Network> currentById)
    {
        var (created, updated, deleted) = ManifestDiffEngine.Compute(
            snapshot, currentById.Values.ToList(), n => n.Id, HasNetworkChanges);

        return new EntityChangeSummary<NetworkRestoreItem>
        {
            Created = created.Select(n => new NetworkRestoreItem(n.Id, n.Name)).ToList(),
            Updated = updated.Select(n => new NetworkRestoreItem(n.Id, n.Name)).ToList(),
            Deleted = deleted.Select(n => new NetworkRestoreItem(n.Id, n.Name)).ToList()
        };
    }

    // Sidecars are matched by Kind, not Id: the manifest carries no Id (it's keyed by Kind on disk),
    // and built-in sidecars are unique per Kind, so Kind is their natural identity for diffing.
    private static EntityChangeSummary<SidecarRestoreItem> ComputeSidecarDiff(
        IReadOnlyList<Sidecar> snapshot,
        Dictionary<SidecarKind, Sidecar> snapshotByKind,
        Dictionary<SidecarKind, Sidecar> currentByKind)
    {
        var (created, updated, deleted) = ManifestDiffEngine.Compute(
            snapshot, currentByKind.Values.ToList(), s => s.Kind, HasSidecarChanges);

        return new EntityChangeSummary<SidecarRestoreItem>
        {
            Created = created.Select(s => new SidecarRestoreItem(s.Id, s.Name)).ToList(),
            Updated = updated.Select(s => new SidecarRestoreItem(currentByKind[s.Kind].Id, s.Name)).ToList(),
            Deleted = deleted.Select(c => new SidecarRestoreItem(c.Id, c.Name)).ToList()
        };
    }

    private async Task ApplyChangesAsync(
        IReadOnlyList<Project> snapshotProjects,
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Project> currentProjectById,
        IReadOnlyList<Environment> snapshotEnvironments,
        Dictionary<Guid, Environment> snapshotEnvironmentById,
        Dictionary<Guid, Environment> currentEnvironmentById,
        IReadOnlyList<Network> snapshotNetworks,
        Dictionary<Guid, Network> snapshotNetworkById,
        Dictionary<Guid, Network> currentNetworkById,
        IReadOnlyList<Sidecar> snapshotSidecars,
        Dictionary<SidecarKind, Sidecar> snapshotSidecarByKind,
        Dictionary<SidecarKind, Sidecar> currentSidecarByKind,
        IReadOnlyList<Service> snapshotServices,
        Dictionary<Guid, Service> snapshotServiceById,
        Dictionary<Guid, Service> currentServiceById,
        IReadOnlyList<EnvironmentVariables> snapshotEnvVars,
        IEnumerable<Guid> snapshotProjectIds,
        IEnumerable<Guid> snapshotEnvironmentIds,
        IEnumerable<Guid> snapshotServiceIds,
        CancellationToken ct)
    {
        var existingTokens = await context.Services
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Token, ct);

        List<ServiceCleanupInfo> deletedServiceCleanupInfo;

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            await ApplyProjectsAsync(snapshotProjects, snapshotProjectById, currentProjectById, ct);
            await ApplyEnvironmentsAsync(snapshotEnvironments, snapshotEnvironmentById, currentEnvironmentById, ct);
            await ApplyNetworksAsync(snapshotNetworks, snapshotNetworkById, currentNetworkById, ct);
            await ApplySidecarsAsync(snapshotSidecars, snapshotSidecarByKind, currentSidecarByKind, ct);
            deletedServiceCleanupInfo = await ApplyServicesAsync(snapshotServices, snapshotServiceById, currentServiceById, ct);
            await ApplyEnvVarsAsync(snapshotEnvVars, snapshotProjectIds, snapshotEnvironmentIds, snapshotServiceIds, ct);
            await RemoveOrphanedEnvironmentVariablesAsync(snapshotProjectIds, snapshotEnvironmentIds, snapshotServiceIds, ct);

            await context.SaveChangesAsync(ct);

            await CreateMissingProjectEnvironmentNetworksAsync(ct);
            await context.SaveChangesAsync(ct);

            RestoreServiceTokens(existingTokens);
            await context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        foreach (var info in deletedServiceCleanupInfo)
            serviceCleanupJobEnqueuer.EnqueueCleanup(info);
    }

    /// <summary>
    /// EnvironmentVariables.ParentId is a plain Guid reference with no FK/cascade configured, so
    /// rows belonging to a Project/Environment/Service removed by this restore are never cleaned
    /// up by cascade delete and would otherwise persist forever. Sweep them here once all
    /// deletes/upserts for this restore have been applied.
    /// </summary>
    private async Task RemoveOrphanedEnvironmentVariablesAsync(
        IEnumerable<Guid> snapshotProjectIds,
        IEnumerable<Guid> snapshotEnvironmentIds,
        IEnumerable<Guid> snapshotServiceIds,
        CancellationToken ct)
    {
        var validParentIds = snapshotProjectIds
            .Concat(snapshotEnvironmentIds)
            .Concat(snapshotServiceIds)
            .ToHashSet();

        await context.EnvironmentVariables
            .Where(v => !validParentIds.Contains(v.ParentId))
            .ExecuteDeleteAsync(ct);
    }

    private async Task ApplyProjectsAsync(
        IReadOnlyList<Project> snapshotProjects,
        Dictionary<Guid, Project> snapshotById,
        Dictionary<Guid, Project> currentById,
        CancellationToken ct)
    {
        var deletedIds = currentById.Keys.Except(snapshotById.Keys).ToList();
        if (deletedIds.Count > 0)
            await context.Projects.Where(p => deletedIds.Contains(p.Id)).ExecuteDeleteAsync(ct);

        foreach (var snapshot in snapshotProjects)
        {
            if (!currentById.ContainsKey(snapshot.Id))
            {
                context.Projects.Add(snapshot);
            }
            else if (HasProjectChanges(snapshot, currentById[snapshot.Id]))
            {
                var tracked = await context.Projects.FindAsync([snapshot.Id], ct);
                if (tracked is not null)
                    context.Entry(tracked).CurrentValues.SetValues(new
                    {
                        snapshot.Name,
                        snapshot.Alias,
                        snapshot.Description
                    });
            }
        }
    }

    private async Task ApplyEnvironmentsAsync(
        IReadOnlyList<Environment> snapshotEnvironments,
        Dictionary<Guid, Environment> snapshotById,
        Dictionary<Guid, Environment> currentById,
        CancellationToken ct)
    {
        var deletedIds = currentById.Keys.Except(snapshotById.Keys).ToList();
        if (deletedIds.Count > 0)
            await context.Environments.Where(e => deletedIds.Contains(e.Id)).ExecuteDeleteAsync(ct);

        foreach (var snapshot in snapshotEnvironments)
        {
            if (!currentById.ContainsKey(snapshot.Id))
            {
                context.Environments.Add(snapshot);
            }
            else if (HasEnvironmentChanges(snapshot, currentById[snapshot.Id]))
            {
                var tracked = await context.Environments.FindAsync([snapshot.Id], ct);
                if (tracked is not null)
                    context.Entry(tracked).CurrentValues.SetValues(new
                    {
                        snapshot.Name,
                        snapshot.Alias,
                        snapshot.Description,
                        snapshot.NetworkName
                    });
            }
        }
    }

    private async Task ApplyNetworksAsync(
        IReadOnlyList<Network> snapshotNetworks,
        Dictionary<Guid, Network> snapshotById,
        Dictionary<Guid, Network> currentById,
        CancellationToken ct)
    {
        var deletedIds = currentById.Keys.Except(snapshotById.Keys).ToList();
        if (deletedIds.Count > 0)
            await context.Networks.Where(n => deletedIds.Contains(n.Id)).ExecuteDeleteAsync(ct);

        foreach (var snapshot in snapshotNetworks)
        {
            if (!currentById.ContainsKey(snapshot.Id))
            {
                // New Shared/External networks bring their manifest-tracked service attachments
                // along as part of the same object graph, so Add() cascades those rows too.
                context.Networks.Add(snapshot);
            }
            else if (HasNetworkChanges(snapshot, currentById[snapshot.Id]))
            {
                var tracked = await context.Networks.FindAsync([snapshot.Id], ct);
                if (tracked is not null)
                    context.Entry(tracked).CurrentValues.SetValues(new
                    {
                        snapshot.Name,
                        snapshot.Metadata
                    });

                if (snapshot.Type != NetworkType.ProjectEnvironment)
                    await ReconcileNetworkServiceAssignmentsAsync(snapshot, ct);
            }
        }
    }

    /// <summary>
    /// Adds/removes service_networks rows for an existing Shared/External network so its service
    /// attachments match the manifest. ProjectEnvironment network membership is implicit (every
    /// service in that environment) and is never reconciled here.
    /// </summary>
    private async Task ReconcileNetworkServiceAssignmentsAsync(Network snapshot, CancellationToken ct)
    {
        var desiredServiceIds = snapshot.ServiceNetworks.Select(sn => sn.ServiceId).ToHashSet();
        var currentAssignments = await context.ServiceNetworks
            .Where(sn => sn.NetworkId == snapshot.Id)
            .ToListAsync(ct);

        var toRemove = currentAssignments.Where(sn => !desiredServiceIds.Contains(sn.ServiceId)).ToList();
        if (toRemove.Count > 0)
            context.ServiceNetworks.RemoveRange(toRemove);

        var currentServiceIds = currentAssignments.Select(sn => sn.ServiceId).ToHashSet();
        foreach (var serviceId in desiredServiceIds.Except(currentServiceIds))
            context.ServiceNetworks.Add(ServiceNetwork.Create(serviceId, snapshot.Id));
    }

    private async Task ApplySidecarsAsync(
        IReadOnlyList<Sidecar> snapshotSidecars,
        Dictionary<SidecarKind, Sidecar> snapshotByKind,
        Dictionary<SidecarKind, Sidecar> currentByKind,
        CancellationToken ct)
    {
        var deletedKinds = currentByKind.Keys.Except(snapshotByKind.Keys).ToList();
        if (deletedKinds.Count > 0)
            await context.Sidecars.Where(s => deletedKinds.Contains(s.Kind)).ExecuteDeleteAsync(ct);

        foreach (var snapshot in snapshotSidecars)
        {
            if (!currentByKind.TryGetValue(snapshot.Kind, out var current))
            {
                context.Sidecars.Add(snapshot);
            }
            else if (HasSidecarChanges(snapshot, current))
            {
                var tracked = await context.Sidecars.FindAsync([current.Id], ct);
                if (tracked is not null)
                    context.Entry(tracked).CurrentValues.SetValues(new
                    {
                        snapshot.Name,
                        snapshot.Alias,
                        snapshot.SourceConfigJson
                    });
            }
        }
    }

    private async Task CreateMissingProjectEnvironmentNetworksAsync(CancellationToken ct)
    {
        var projects = await context.Projects.Include(p => p.Environments).ToListAsync(ct);
        var existingNetworks = await context.Networks
            .Where(n => n.Type == NetworkType.ProjectEnvironment)
            .ToListAsync(ct);

        foreach (var project in projects)
        {
            foreach (var environment in project.Environments)
            {
                var hasNetwork = existingNetworks.Any(n =>
                    n.ProjectId == project.Id && n.EnvironmentId == environment.Id);

                if (hasNetwork) continue;

                try
                {
                    context.Networks.Add(Network.CreateProjectEnvironmentNetwork(
                        project.Id, project.Name, environment.Id, environment.Name));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to create missing network for project '{Project}' environment '{Environment}'",
                        project.Name, environment.Name);
                }
            }
        }
    }

    private static EntityChangeSummary<ServiceRestoreItem> ComputeServiceDiff(
        IReadOnlyList<Service> snapshot,
        Dictionary<Guid, Service> snapshotById,
        Dictionary<Guid, Service> currentById,
        Dictionary<Guid, Environment> snapshotEnvironmentById,
        Dictionary<Guid, Environment> currentEnvironmentById,
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Project> currentProjectById,
        IReadOnlySet<Guid> servicesWithVolumeFileChanges)
    {
        static ServiceRestoreItem ToItem(Service s, Dictionary<Guid, Environment> envById, Dictionary<Guid, Project> projById)
        {
            var env = envById.GetValueOrDefault(s.EnvironmentId);
            var proj = env is not null ? projById.GetValueOrDefault(env.ProjectId) : null;
            return new ServiceRestoreItem(s.Id, s.Name, s.EnvironmentId, env?.ProjectId ?? Guid.Empty, env?.Name, proj?.Name);
        }

        return new()
        {
            Created = snapshot.Where(s => !currentById.ContainsKey(s.Id))
                .Select(s => ToItem(s, snapshotEnvironmentById, snapshotProjectById)).ToList(),
            Updated = snapshot.Where(s => currentById.TryGetValue(s.Id, out var cur)
                    && (HasServiceChanges(s, cur) || HasServiceVolumeChanges(s, cur) || servicesWithVolumeFileChanges.Contains(s.Id)))
                .Select(s => ToItem(s, snapshotEnvironmentById, snapshotProjectById)).ToList(),
            Deleted = currentById.Values.Where(s => !snapshotById.ContainsKey(s.Id))
                .Select(s => ToItem(s, currentEnvironmentById, currentProjectById)).ToList()
        };
    }

    private async Task<List<ServiceCleanupInfo>> ApplyServicesAsync(
        IReadOnlyList<Service> snapshotServices,
        Dictionary<Guid, Service> snapshotById,
        Dictionary<Guid, Service> currentById,
        CancellationToken ct)
    {
        var deletedIds = currentById.Keys.Except(snapshotById.Keys).ToList();
        var deletedServiceCleanupInfo = deletedIds
            .Select(id => currentById[id])
            .Select(s => new ServiceCleanupInfo(s.Id, s.Name, s.Alias, s.Type, s.SourceConfigJson))
            .ToList();

        // context.Services.Where(...).ExecuteDeleteAsync bypasses the change tracker, so
        // ServiceDeletedEvent never fires here - the caller enqueues a background cleanup job
        // per deletedServiceCleanupInfo entry once the transaction has committed.
        if (deletedIds.Count > 0)
            await context.Services.Where(s => deletedIds.Contains(s.Id)).ExecuteDeleteAsync(ct);

        foreach (var snapshot in snapshotServices)
        {
            if (!currentById.ContainsKey(snapshot.Id))
            {
                snapshot.Environment = null; // avoid EF tracking conflict with already-tracked Environment instances
                context.Services.Add(snapshot);
            }
            else
            {
                if (HasServiceChanges(snapshot, currentById[snapshot.Id]))
                {
                    var tracked = await context.Services.FindAsync([snapshot.Id], ct);
                    if (tracked is not null)
                    {
                        context.Entry(tracked).CurrentValues.SetValues(new
                        {
                            snapshot.Name,
                            snapshot.Alias,
                            snapshot.Type,
                            snapshot.ExposureMode,
                            snapshot.SourceConfigJson
                        });

                        await context.FeatureFlags.Where(f => f.ServiceId == snapshot.Id).ExecuteDeleteAsync(ct);
                        foreach (var flag in snapshot.FeatureFlags)
                            context.FeatureFlags.Add(flag);
                    }
                }

                // Volumes are restored independently of scalar service changes: a backup whose only
                // difference is its volumes must still apply. Upsert is idempotent and by id.
                await UpsertVolumesAsync(snapshot, ct);
            }
        }

        return deletedServiceCleanupInfo;
    }

    /// <summary>
    /// Upserts the snapshot's (backup-enabled) volumes by id. Volumes not present in the snapshot
    /// are left untouched, so volumes the user opted out of backing up are not lost on restore.
    /// </summary>
    private async Task UpsertVolumesAsync(Service snapshot, CancellationToken ct)
    {
        foreach (var volume in snapshot.Volumes)
        {
            var existing = await context.ServiceVolumes.FindAsync([volume.Id], ct);
            if (existing is null)
            {
                context.ServiceVolumes.Add(volume);
            }
            else
            {
                context.Entry(existing).CurrentValues.SetValues(new
                {
                    volume.Type,
                    volume.Name,
                    volume.Source,
                    volume.Target,
                    volume.ReadOnly,
                    volume.BackupEnabled
                });
            }
        }
    }

    /// <summary>
    /// Restores managed-volume files: deletes the volume directories of services removed by this
    /// restore, then copies each backed-up managed volume's files from the snapshot's
    /// <c>volumes/{name}/</c> side-car into <c>{VolumesRoot}/{serviceId}/{volumeId}</c>.
    /// </summary>
    /// <summary>
    /// Restores managed-volume files on disk. The DB changes for this restore have already been
    /// committed by the time this runs, so a failure here can't be rolled back - instead, each
    /// service/volume is restored independently and any failure is logged and collected as a
    /// warning rather than thrown, so a single bad volume doesn't abort the rest of the restore
    /// or mask that the DB/manifest changes already succeeded.
    /// </summary>
    private List<string> RestoreVolumeFiles(
        string sourceDir,
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Environment> snapshotEnvironmentById,
        Dictionary<Guid, Service> snapshotServiceById,
        Dictionary<Guid, Service> currentServiceById)
    {
        var root = volumesOptions.CurrentValue.RootPath;
        var warnings = new List<string>();

        foreach (var deletedId in currentServiceById.Keys.Except(snapshotServiceById.Keys))
        {
            try
            {
                var serviceVolumeDir = Path.GetFullPath(Path.Combine(root, deletedId.ToString()));
                if (Directory.Exists(serviceVolumeDir))
                    Directory.Delete(serviceVolumeDir, recursive: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete volume files for removed service {ServiceId}", deletedId);
                warnings.Add($"Failed to delete volume files for removed service '{deletedId}': {ex.Message}");
            }
        }

        foreach (var service in snapshotServiceById.Values)
        {
            if (!snapshotEnvironmentById.TryGetValue(service.EnvironmentId, out var environment)) continue;
            if (!snapshotProjectById.TryGetValue(environment.ProjectId, out var project)) continue;

            var volumesSnapshotDir = Path.Combine(
                sourceDir, "projects", project.Name,
                PathResolver.EnvironmentDirectory, environment.Name,
                PathResolver.ServiceDirectory, service.Name,
                "volumes");

            if (!Directory.Exists(volumesSnapshotDir)) continue;

            foreach (var volume in service.Volumes.Where(v => v.Type == VolumeType.Managed))
            {
                var volumeSnapshotDir = Path.Combine(volumesSnapshotDir, volume.Name);
                if (!Directory.Exists(volumeSnapshotDir)) continue;

                try
                {
                    var destDir = DockerUtils.ManagedVolumeHostPath(root, service.Id, volume.Id);
                    if (Directory.Exists(destDir))
                        Directory.Delete(destDir, recursive: true);

                    DirectoryUtils.CopyDirectory(volumeSnapshotDir, destDir);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to restore files for volume '{VolumeName}' on service '{ServiceName}'", volume.Name, service.Name);
                    warnings.Add($"Failed to restore files for volume '{volume.Name}' on service '{service.Name}': {ex.Message}");
                }
            }
        }

        return warnings;
    }

    private async Task<List<EnvironmentVariables>> ReadSnapshotEnvVarsAsync(
        string sourceDir,
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Environment> snapshotEnvironmentById,
        Dictionary<Guid, Service> snapshotServiceById,
        CancellationToken ct)
    {
        var projectsByName = snapshotProjectById.Values.ToDictionary(p => p.Name);
        var environmentsByKey = snapshotEnvironmentById.Values
            .Where(e => snapshotProjectById.ContainsKey(e.ProjectId))
            .ToDictionary(e => (snapshotProjectById[e.ProjectId].Name, e.Name));

        var servicesByKey = new Dictionary<(string Project, string Environment, string Service), Service>();
        foreach (var svc in snapshotServiceById.Values)
        {
            if (!snapshotEnvironmentById.TryGetValue(svc.EnvironmentId, out var env)) continue;
            if (!snapshotProjectById.TryGetValue(env.ProjectId, out var proj)) continue;
            servicesByKey[(proj.Name, env.Name, svc.Name)] = svc;
        }

        var vars = new List<EnvironmentVariables>();
        var projectsPath = Path.Combine(sourceDir, "projects");
        if (!Directory.Exists(projectsPath)) return vars;

        foreach (var projectDir in Directory.EnumerateDirectories(projectsPath))
        {
            var projectName = Path.GetFileName(projectDir);
            if (!projectsByName.TryGetValue(projectName, out var project)) continue;

            var projectEnvFile = Path.Combine(projectDir, PathResolver.EnvExampleFile);
            if (File.Exists(projectEnvFile))
            {
                var content = await File.ReadAllTextAsync(projectEnvFile, ct);
                vars.AddRange(EnvironmentVariableConverter.Convert(content, project.Id, EnvironmentVariableParentType.Project));
            }

            var environmentsPath = Path.Combine(projectDir, PathResolver.EnvironmentDirectory);
            if (!Directory.Exists(environmentsPath)) continue;

            foreach (var environmentDir in Directory.EnumerateDirectories(environmentsPath))
            {
                var envName = Path.GetFileName(environmentDir);
                if (!environmentsByKey.TryGetValue((projectName, envName), out var environment)) continue;

                var envVarFile = Path.Combine(environmentDir, PathResolver.EnvExampleFile);
                if (File.Exists(envVarFile))
                {
                    var content = await File.ReadAllTextAsync(envVarFile, ct);
                    vars.AddRange(EnvironmentVariableConverter.Convert(content, environment.Id, EnvironmentVariableParentType.Environment));
                }

                var servicesPath = Path.Combine(environmentDir, PathResolver.ServiceDirectory);
                if (!Directory.Exists(servicesPath)) continue;

                foreach (var serviceDir in Directory.EnumerateDirectories(servicesPath))
                {
                    var serviceName = Path.GetFileName(serviceDir);
                    if (!servicesByKey.TryGetValue((projectName, envName, serviceName), out var service)) continue;

                    var serviceEnvFile = Path.Combine(serviceDir, PathResolver.EnvExampleFile);
                    if (File.Exists(serviceEnvFile))
                    {
                        var content = await File.ReadAllTextAsync(serviceEnvFile, ct);
                        vars.AddRange(EnvironmentVariableConverter.Convert(content, service.Id, EnvironmentVariableParentType.Service));
                    }
                }
            }
        }

        EncryptedEnvValue.DecryptInPlace(vars, encryptionService);
        return vars;
    }

    private static EntityChangeSummary<EnvVarRestoreItem> ComputeEnvVarDiff(
        IReadOnlyList<EnvironmentVariables> snapshot,
        IReadOnlyList<EnvironmentVariables> current,
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Project> currentProjectById,
        Dictionary<Guid, Environment> snapshotEnvironmentById,
        Dictionary<Guid, Environment> currentEnvironmentById,
        Dictionary<Guid, Service> snapshotServiceById,
        Dictionary<Guid, Service> currentServiceById)
    {
        var currentByKey = current.ToDictionary(v => (v.ParentId, v.Key));
        var snapshotByKey = snapshot.ToDictionary(v => (v.ParentId, v.Key));

        string? ResolveName(Guid parentId, bool fromSnapshot)
        {
            if (fromSnapshot)
                return (snapshotProjectById.GetValueOrDefault(parentId)?.Name
                    ?? snapshotEnvironmentById.GetValueOrDefault(parentId)?.Name
                    ?? snapshotServiceById.GetValueOrDefault(parentId)?.Name);
            return (currentProjectById.GetValueOrDefault(parentId)?.Name
                ?? currentEnvironmentById.GetValueOrDefault(parentId)?.Name
                ?? currentServiceById.GetValueOrDefault(parentId)?.Name);
        }

        return new()
        {
            Created = snapshot.Where(v => !currentByKey.ContainsKey((v.ParentId, v.Key)))
                .Select(v => new EnvVarRestoreItem(v.Key, v.ParentId, ResolveName(v.ParentId, true))).ToList(),
            Updated = snapshot.Where(v => currentByKey.TryGetValue((v.ParentId, v.Key), out var cur) && cur.Value != v.Value)
                .Select(v => new EnvVarRestoreItem(v.Key, v.ParentId, ResolveName(v.ParentId, true))).ToList(),
            Deleted = current.Where(v => !snapshotByKey.ContainsKey((v.ParentId, v.Key)))
                .Select(v => new EnvVarRestoreItem(v.Key, v.ParentId, ResolveName(v.ParentId, false))).ToList()
        };
    }

    private async Task ApplyEnvVarsAsync(
        IReadOnlyList<EnvironmentVariables> snapshotEnvVars,
        IEnumerable<Guid> snapshotProjectIds,
        IEnumerable<Guid> snapshotEnvironmentIds,
        IEnumerable<Guid> snapshotServiceIds,
        CancellationToken ct)
    {
        var allSnapshotParentIds = snapshotProjectIds
            .Concat(snapshotEnvironmentIds)
            .Concat(snapshotServiceIds)
            .ToList();

        if (allSnapshotParentIds.Count > 0)
            await context.EnvironmentVariables
                .Where(v => allSnapshotParentIds.Contains(v.ParentId))
                .ExecuteDeleteAsync(ct);

        context.EnvironmentVariables.AddRange(snapshotEnvVars);
    }

    private static bool HasProjectChanges(Project s, Project c)
        => s.Name != c.Name || s.Alias != c.Alias || s.Description != c.Description;

    private static bool HasEnvironmentChanges(Environment s, Environment c)
        => s.Name != c.Name || s.Alias != c.Alias || s.Description != c.Description || s.NetworkName != c.NetworkName;

    private static bool HasNetworkChanges(Network s, Network c)
    {
        if (s.Name != c.Name || s.Type != c.Type || s.Metadata != c.Metadata)
            return true;

        // ProjectEnvironment network membership is implicit (every service in that environment),
        // not manifest-driven, so it's never part of this comparison.
        if (s.Type == NetworkType.ProjectEnvironment)
            return false;

        var snapshotServiceIds = s.ServiceNetworks.Select(sn => sn.ServiceId).ToHashSet();
        var currentServiceIds = c.ServiceNetworks.Select(sn => sn.ServiceId).ToHashSet();
        return !snapshotServiceIds.SetEquals(currentServiceIds);
    }

    private static bool HasSidecarChanges(Sidecar s, Sidecar c)
        => s.Name != c.Name || s.Alias != c.Alias || s.Kind != c.Kind || s.SourceConfigJson != c.SourceConfigJson;

    private static bool HasServiceChanges(Service s, Service c)
        => s.Name != c.Name || s.Alias != c.Alias || s.Type != c.Type || s.ExposureMode != c.ExposureMode || s.SourceConfigJson != c.SourceConfigJson;

    /// <summary>
    /// True if any of the snapshot's (backup-enabled) volumes is new or differs from the current
    /// state. Mirrors what <see cref="UpsertVolumesAsync"/> applies — it upserts by id and never
    /// deletes, so removed non-backed-up volumes are not counted as a change.
    /// </summary>
    private static bool HasServiceVolumeChanges(Service snapshot, Service current)
    {
        var currentById = current.Volumes.ToDictionary(v => v.Id);
        foreach (var volume in snapshot.Volumes)
        {
            if (!currentById.TryGetValue(volume.Id, out var existing))
                return true;

            if (existing.Type != volume.Type
                || existing.Name != volume.Name
                || existing.Source != volume.Source
                || existing.Target != volume.Target
                || existing.ReadOnly != volume.ReadOnly
                || existing.BackupEnabled != volume.BackupEnabled)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Computes the per-file changes a restore would apply to managed volumes: each snapshot
    /// side-car (<c>volumes/{name}/</c>) is compared against the live
    /// <c>{VolumesRoot}/{serviceId}/{volumeId}</c>. Files only in the snapshot are created, files
    /// that differ are updated, and files only on disk are deleted (restore replaces the directory).
    /// The displayed path is prefixed with the volume name.
    /// </summary>
    private EntityChangeSummary<VolumeFileRestoreItem> ComputeVolumeFileDiff(
        string sourceDir,
        Dictionary<Guid, Project> snapshotProjectById,
        Dictionary<Guid, Environment> snapshotEnvironmentById,
        Dictionary<Guid, Service> snapshotServiceById)
    {
        var created = new List<VolumeFileRestoreItem>();
        var updated = new List<VolumeFileRestoreItem>();
        var deleted = new List<VolumeFileRestoreItem>();
        var root = volumesOptions.CurrentValue.RootPath;

        foreach (var service in snapshotServiceById.Values)
        {
            if (!snapshotEnvironmentById.TryGetValue(service.EnvironmentId, out var environment)) continue;
            if (!snapshotProjectById.TryGetValue(environment.ProjectId, out var project)) continue;

            var volumesSnapshotDir = Path.Combine(
                sourceDir, "projects", project.Name,
                PathResolver.EnvironmentDirectory, environment.Name,
                PathResolver.ServiceDirectory, service.Name,
                "volumes");

            foreach (var volume in service.Volumes.Where(v => v.Type == VolumeType.Managed))
            {
                var snapshotVolumeDir = Path.Combine(volumesSnapshotDir, volume.Name);

                // A volume with no side-car in the snapshot is left untouched by restore.
                if (!Directory.Exists(snapshotVolumeDir)) continue;

                var liveVolumeDir = DockerUtils.ManagedVolumeHostPath(root, service.Id, volume.Id);
                var snapshotFiles = EnumerateRelativeFiles(snapshotVolumeDir);
                var liveFiles = EnumerateRelativeFiles(liveVolumeDir);

                foreach (var (relative, snapshotPath) in snapshotFiles)
                {
                    var item = new VolumeFileRestoreItem($"{volume.Name}/{relative}", service.Id, volume.Name, service.Name);
                    if (!liveFiles.TryGetValue(relative, out var livePath))
                        created.Add(item);
                    else if (!FilesEqual(snapshotPath, livePath))
                        updated.Add(item);
                }

                foreach (var relative in liveFiles.Keys)
                {
                    if (!snapshotFiles.ContainsKey(relative))
                        deleted.Add(new VolumeFileRestoreItem($"{volume.Name}/{relative}", service.Id, volume.Name, service.Name));
                }
            }
        }

        return new EntityChangeSummary<VolumeFileRestoreItem>
        {
            Created = created,
            Updated = updated,
            Deleted = deleted
        };
    }

    private static Dictionary<string, string> EnumerateRelativeFiles(string dir)
    {
        if (!Directory.Exists(dir))
            return [];

        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(dir, f).Replace(Path.DirectorySeparatorChar, '/'));
    }

    private static bool FilesEqual(string a, string b)
    {
        var infoA = new FileInfo(a);
        var infoB = new FileInfo(b);
        if (infoA.Length != infoB.Length)
            return false;

        return File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));
    }

    private void RestoreServiceTokens(Dictionary<Guid, string> existingTokens)
    {
        foreach (var service in context.Services.Local)
        {
            if (existingTokens.TryGetValue(service.Id, out var token))
                service.Token = token;
        }
    }
}