using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Converters;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Backup;

public sealed class BackupManifestWriter(
    IEnumerable<IManifestEntitySerializer> serializers,
    HavenDbContext context,
    IEncryptionService encryptionService,
    ILogger<BackupManifestWriter> logger) : IBackupManifestWriter
{
    private readonly IReadOnlyDictionary<Type, IManifestEntitySerializer> _serializerMap =
        serializers.ToDictionary(s => s.EntityType);

    public async Task WriteAllAsync(string targetBasePath, CancellationToken ct = default)
    {
        logger.LogInformation("Writing full platform state to {TargetBasePath}", targetBasePath);

        var parent = Path.GetDirectoryName(Path.GetFullPath(targetBasePath))
            ?? throw new InvalidOperationException($"Could not resolve parent directory for '{targetBasePath}'.");
        var stagingPath = Path.Combine(parent, $"{Path.GetFileName(targetBasePath)}.tmp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingPath);

        try
        {
            var projects = await context.Projects
                .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
                .ThenInclude(s => s.FeatureFlags)
                .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
                .ThenInclude(s => s.Volumes)
                .AsNoTracking()
                .ToListAsync(ct);

            // System networks (the single auto-regenerated control-plane network) are excluded -
            // everything else, including user-created Shared/External networks and their attached
            // services, is real configuration and must be backed up.
            var networks = await context.Networks
                .Where(n => n.Type != NetworkType.System)
                .Include(n => n.Project)
                .Include(n => n.Environment)
                .Include(n => n.ServiceNetworks)
                .AsNoTracking()
                .ToListAsync(ct);

            var sidecars = await context.Sidecars.AsNoTracking().ToListAsync(ct);

            var envVarsByParentId = (await context.EnvironmentVariables.AsNoTracking().ToListAsync(ct))
                .GroupBy(v => v.ParentId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Entities.EnvironmentVariables>)g.ToList());

            foreach (var project in projects)
            {
                await WriteAsync(project, stagingPath, ct);
                await WriteEnvExampleAsync(
                    PathResolver.ProjectEnvExamplePath(stagingPath, project.Name), envVarsByParentId, project.Id, ct);

                foreach (var environment in project.Environments)
                {
                    await WriteAsync(environment, stagingPath, ct);
                    await WriteEnvExampleAsync(
                        PathResolver.EnvironmentEnvExamplePath(stagingPath, project.Name, environment.Name),
                        envVarsByParentId, environment.Id, ct);

                    foreach (var service in environment.Services)
                    {
                        await WriteAsync(service, stagingPath, ct);
                        await WriteEnvExampleAsync(
                            PathResolver.ServiceEnvExamplePath(stagingPath, project.Name, environment.Name, service.Name),
                            envVarsByParentId, service.Id, ct);
                    }
                }

            }

            foreach (var network in networks)
            {
                if (network.Type != NetworkType.ProjectEnvironment || (network.Project is not null && network.Environment is not null))
                    await WriteAsync(network, stagingPath, ct);
            }

            foreach (var sidecar in sidecars)
                await WriteAsync(sidecar, stagingPath, ct);

            SwapInStagingDirectory(stagingPath, targetBasePath);
        }
        catch
        {
            if (Directory.Exists(stagingPath))
            {
                try { Directory.Delete(stagingPath, recursive: true); }
                catch (Exception cleanupEx)
                {
                    logger.LogWarning(cleanupEx, "Failed to clean up staging directory {StagingPath} after a failed write", stagingPath);
                }
            }

            throw;
        }

        logger.LogInformation("Platform state written successfully to {TargetBasePath}", targetBasePath);
    }

    /// <summary>
    /// Swaps freshly-written content into the target directory without disturbing a ".git" folder
    /// that may live there. The live manifests directory is now resynced this way on every mutating
    /// command (debounced), not just from a manual "create backup" - a full delete-and-replace of
    /// the whole directory would wipe .git along with it, destroying the entire commit history on
    /// the very first mutation after git backup is enabled.
    /// </summary>
    private static void SwapInStagingDirectory(string stagingPath, string targetBasePath)
    {
        if (!Directory.Exists(targetBasePath))
        {
            Directory.Move(stagingPath, targetBasePath);
            return;
        }

        foreach (var entry in Directory.GetFileSystemEntries(targetBasePath))
        {
            if (Path.GetFileName(entry) == ".git")
                continue;

            if (Directory.Exists(entry))
                Directory.Delete(entry, recursive: true);
            else
                File.Delete(entry);
        }

        foreach (var entry in Directory.GetFileSystemEntries(stagingPath))
        {
            var destination = Path.Combine(targetBasePath, Path.GetFileName(entry));
            if (Directory.Exists(entry))
                Directory.Move(entry, destination);
            else
                File.Move(entry, destination);
        }

        Directory.Delete(stagingPath, recursive: true);
    }

    private Task WriteAsync(object entity, string basePath, CancellationToken ct)
    {
        if (!_serializerMap.TryGetValue(entity.GetType(), out var serializer))
        {
            logger.LogWarning("No manifest serializer registered for {EntityType}", entity.GetType().Name);
            return Task.CompletedTask;
        }

        return serializer.WriteToAsync(entity, basePath, ct);
    }

    private async Task WriteEnvExampleAsync(
        string path,
        IReadOnlyDictionary<Guid, IReadOnlyList<Domain.Entities.EnvironmentVariables>> envVarsByParentId,
        Guid parentId,
        CancellationToken ct)
    {
        if (!envVarsByParentId.TryGetValue(parentId, out var variables) || variables.Count == 0)
            return;

        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        var encrypted = EncryptedEnvValue.EncryptAll(variables, encryptionService);
        var content = EnvironmentVariableConverter.Convert(encrypted, includeValues: true);
        await File.WriteAllTextAsync(path, content, ct);
    }
}