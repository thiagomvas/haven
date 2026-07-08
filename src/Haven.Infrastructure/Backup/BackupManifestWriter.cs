using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Backup;

public sealed class BackupManifestWriter(
    IEnumerable<IManifestEntitySerializer> serializers,
    HavenDbContext context,
    ILogger<BackupManifestWriter> logger) : IBackupManifestWriter
{
    private readonly IReadOnlyDictionary<Type, IManifestEntitySerializer> _serializerMap =
        serializers.ToDictionary(s => s.EntityType);

    public async Task WriteAllAsync(string targetBasePath, CancellationToken ct = default)
    {
        logger.LogInformation("Writing full platform state to {TargetBasePath}", targetBasePath);

        if (Directory.Exists(targetBasePath))
        {
            logger.LogWarning("Target base path {TargetBasePath} already exists. Deleting it before writing the backup.", targetBasePath);
            Directory.Delete(targetBasePath, recursive: true);
            Directory.CreateDirectory(targetBasePath);
        }

        var projects = await context.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .ThenInclude(s => s.FeatureFlags)
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .ThenInclude(s => s.Volumes)
            .AsNoTracking()
            .ToListAsync(ct);

        var networks = await context.Networks
            .Where(n => n.Type == Domain.NetworkType.ProjectEnvironment)
            .Include(n => n.Project)
            .Include(n => n.Environment)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var project in projects)
        {
            await WriteAsync(project, targetBasePath, ct);

            foreach (var environment in project.Environments)
            {
                await WriteAsync(environment, targetBasePath, ct);

                foreach (var service in environment.Services)
                    await WriteAsync(service, targetBasePath, ct);
            }
        }

        foreach (var network in networks)
        {
            if (network.Project is not null && network.Environment is not null)
                await WriteAsync(network, targetBasePath, ct);
        }

        logger.LogInformation("Platform state written successfully to {TargetBasePath}", targetBasePath);
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
}