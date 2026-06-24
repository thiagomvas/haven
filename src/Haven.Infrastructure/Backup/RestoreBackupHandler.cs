using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Backup;

public sealed class RestoreBackupHandler(
    IBackupManifestReader manifestReader,
    IManifestSerializer<Project> projectSerializer,
    IManifestSerializer<Environment> environmentSerializer,
    IManifestSerializer<Network> networkSerializer,
    HavenDbContext context,
    ILogger<RestoreBackupHandler> logger)
    : ICommandHandler<RestoreBackupCommand, RestoreBackupResult>
{
    public async ValueTask<Result<RestoreBackupResult>> Handle(RestoreBackupCommand request, CancellationToken ct)
    {
        string? tempDir = null;
        try
        {
            var sourceDir = await manifestReader.PrepareSourceDirectoryAsync(
                request.Source, request.SnapshotName, request.CommitSha, ct);

            if (request.Source == RestoreSource.Git)
                tempDir = sourceDir;

            var snapshotProjects = await projectSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotProjectById = snapshotProjects.ToDictionary(p => p.Id);

            var snapshotEnvironments = await environmentSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotEnvironmentById = snapshotEnvironments.ToDictionary(e => e.Id);

            var snapshotNetworks = await networkSerializer.ReadFromAsync(sourceDir, ct: ct);
            var snapshotNetworkById = snapshotNetworks.ToDictionary(n => n.Id);

            var currentProjects = await context.Projects.AsNoTracking().ToListAsync(ct);
            var currentProjectById = currentProjects.ToDictionary(p => p.Id);

            var currentEnvironments = await context.Environments.AsNoTracking().ToListAsync(ct);
            var currentEnvironmentById = currentEnvironments.ToDictionary(e => e.Id);

            var currentNetworks = await context.Networks.AsNoTracking().ToListAsync(ct);
            var currentNetworkById = currentNetworks.ToDictionary(n => n.Id);

            var projectsDiff = ComputeProjectDiff(snapshotProjects, snapshotProjectById, currentProjectById);
            var environmentsDiff = ComputeEnvironmentDiff(snapshotEnvironments, snapshotEnvironmentById, currentEnvironmentById);
            var networksDiff = ComputeNetworkDiff(snapshotNetworks, snapshotNetworkById, currentNetworkById);

            if (!request.DryRun)
                await ApplyChangesAsync(
                    snapshotProjects, snapshotProjectById, currentProjectById,
                    snapshotEnvironments, snapshotEnvironmentById, currentEnvironmentById,
                    snapshotNetworks, snapshotNetworkById, currentNetworkById,
                    ct);

            logger.LogInformation(
                "Restore (DryRun={DryRun}): projects +{PC}~{PU}-{PD}, environments +{EC}~{EU}-{ED}, networks +{NC}~{NU}-{ND}",
                request.DryRun,
                projectsDiff.Created.Count, projectsDiff.Updated.Count, projectsDiff.Deleted.Count,
                environmentsDiff.Created.Count, environmentsDiff.Updated.Count, environmentsDiff.Deleted.Count,
                networksDiff.Created.Count, networksDiff.Updated.Count, networksDiff.Deleted.Count);

            return Result<RestoreBackupResult>.Success(new RestoreBackupResult
            {
                DryRun = request.DryRun,
                Projects = projectsDiff,
                Environments = environmentsDiff,
                Networks = networksDiff
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
        }
    }

    private static EntityChangeSummary<ProjectRestoreItem> ComputeProjectDiff(
        IReadOnlyList<Project> snapshot,
        Dictionary<Guid, Project> snapshotById,
        Dictionary<Guid, Project> currentById) => new()
        {
            Created = snapshot.Where(p => !currentById.ContainsKey(p.Id))
                .Select(p => new ProjectRestoreItem(p.Id, p.Name)).ToList(),
            Updated = snapshot.Where(p => currentById.TryGetValue(p.Id, out var cur) && HasProjectChanges(p, cur))
                .Select(p => new ProjectRestoreItem(p.Id, p.Name)).ToList(),
            Deleted = currentById.Values.Where(p => !snapshotById.ContainsKey(p.Id))
                .Select(p => new ProjectRestoreItem(p.Id, p.Name)).ToList()
        };

    private static EntityChangeSummary<EnvironmentRestoreItem> ComputeEnvironmentDiff(
        IReadOnlyList<Environment> snapshot,
        Dictionary<Guid, Environment> snapshotById,
        Dictionary<Guid, Environment> currentById) => new()
        {
            Created = snapshot.Where(e => !currentById.ContainsKey(e.Id))
                .Select(e => new EnvironmentRestoreItem(e.Id, e.Name, e.ProjectId)).ToList(),
            Updated = snapshot.Where(e => currentById.TryGetValue(e.Id, out var cur) && HasEnvironmentChanges(e, cur))
                .Select(e => new EnvironmentRestoreItem(e.Id, e.Name, e.ProjectId)).ToList(),
            Deleted = currentById.Values.Where(e => !snapshotById.ContainsKey(e.Id))
                .Select(e => new EnvironmentRestoreItem(e.Id, e.Name, e.ProjectId)).ToList()
        };

    private static EntityChangeSummary<NetworkRestoreItem> ComputeNetworkDiff(
        IReadOnlyList<Network> snapshot,
        Dictionary<Guid, Network> snapshotById,
        Dictionary<Guid, Network> currentById) => new()
        {
            Created = snapshot.Where(n => !currentById.ContainsKey(n.Id))
                .Select(n => new NetworkRestoreItem(n.Id, n.Name)).ToList(),
            Updated = snapshot.Where(n => currentById.TryGetValue(n.Id, out var cur) && HasNetworkChanges(n, cur))
                .Select(n => new NetworkRestoreItem(n.Id, n.Name)).ToList(),
            Deleted = currentById.Values.Where(n => !snapshotById.ContainsKey(n.Id))
                .Select(n => new NetworkRestoreItem(n.Id, n.Name)).ToList()
        };

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
        CancellationToken ct)
    {
        var existingTokens = await context.Services
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Token, ct);

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            await ApplyProjectsAsync(snapshotProjects, snapshotProjectById, currentProjectById, ct);
            await ApplyEnvironmentsAsync(snapshotEnvironments, snapshotEnvironmentById, currentEnvironmentById, ct);
            await ApplyNetworksAsync(snapshotNetworks, snapshotNetworkById, currentNetworkById, ct);

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

    private static bool HasProjectChanges(Project s, Project c)
        => s.Name != c.Name || s.Alias != c.Alias || s.Description != c.Description;

    private static bool HasEnvironmentChanges(Environment s, Environment c)
        => s.Name != c.Name || s.Alias != c.Alias || s.Description != c.Description || s.NetworkName != c.NetworkName;

    private static bool HasNetworkChanges(Network s, Network c)
        => s.Name != c.Name || s.Type != c.Type || s.Metadata != c.Metadata;

    private void RestoreServiceTokens(Dictionary<Guid, string> existingTokens)
    {
        foreach (var service in context.Services.Local)
        {
            if (existingTokens.TryGetValue(service.Id, out var token))
                service.Token = token;
        }
    }
}
