using Haven.Application.Common.Interfaces;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Manifests;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using YamlDotNet.Serialization;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Backup;

public sealed class BackupManifestWriter(
    HavenDbContext context,
    ILogger<BackupManifestWriter> logger) : IBackupManifestWriter
{
    private readonly ISerializer _serializer = YamlSerializerPresets.CreateSerializer();

    public async Task WriteAllAsync(string targetBasePath, CancellationToken ct = default)
    {
        logger.LogInformation("Writing full platform state to {TargetBasePath}", targetBasePath);

        var projects = await context.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .ThenInclude(s => s.FeatureFlags)
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
            await WriteProjectAsync(project, targetBasePath, ct);

            foreach (var environment in project.Environments)
            {
                await WriteEnvironmentAsync(project, environment, targetBasePath, ct);

                foreach (var service in environment.Services)
                {
                    await WriteServiceAsync(project, environment, service, targetBasePath, ct);
                }
            }
        }

        foreach (var network in networks)
        {
            if (network.Project is not null && network.Environment is not null)
                await WriteNetworkAsync(network.Project, network.Environment, network, targetBasePath, ct);
        }

        logger.LogInformation("Platform state written successfully to {TargetBasePath}", targetBasePath);
    }

    private async Task WriteProjectAsync(Project project, string basePath, CancellationToken ct)
    {
        var dir = Path.Combine(basePath, "projects", project.Name);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, PathResolver.ProjectFile);
        var yaml = _serializer.Serialize(project.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogDebug("Wrote project manifest to {FilePath}", filePath);
    }

    private async Task WriteEnvironmentAsync(Project project, Environment environment, string basePath, CancellationToken ct)
    {
        var dir = Path.Combine(basePath, "projects", project.Name, PathResolver.EnvironmentDirectory, environment.Name);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, PathResolver.EnvironmentFile);
        var yaml = _serializer.Serialize(environment.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogDebug("Wrote environment manifest to {FilePath}", filePath);
    }

    private async Task WriteServiceAsync(Project project, Environment environment, Domain.Entities.Service service, string basePath, CancellationToken ct)
    {
        var dir = Path.Combine(basePath, "projects", project.Name, PathResolver.EnvironmentDirectory, environment.Name, PathResolver.ServiceDirectory, service.Name);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, PathResolver.ServiceFile);
        var yaml = _serializer.Serialize(service.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogDebug("Wrote service manifest to {FilePath}", filePath);
    }

    private async Task WriteNetworkAsync(Project project, Environment environment, Network network, string basePath, CancellationToken ct)
    {
        var dir = Path.Combine(basePath, "projects", project.Name, PathResolver.EnvironmentDirectory, environment.Name);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, PathResolver.NetworkFile);
        var yaml = _serializer.Serialize(network.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogDebug("Wrote network manifest to {FilePath}", filePath);
    }
}
