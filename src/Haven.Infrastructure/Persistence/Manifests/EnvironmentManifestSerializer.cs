using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects;
using Haven.Application.Mappers;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using YamlDotNet.Serialization;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Persistence.Manifests;

public class EnvironmentManifestSerializer(IProjectRepository projectRepository, ILogger<EnvironmentManifestSerializer> logger) : IManifestSerializer<Environment>
{
    private readonly ISerializer _serializer = YamlSerializerPresets.CreateSerializer();
    private readonly IDeserializer _deserializer = YamlSerializerPresets.CreateDeserializer();
    public Type EntityType => typeof(Environment);

    Task IManifestEntitySerializer.WriteToAsync(object item, string basePath, CancellationToken ct)
        => WriteToAsync((Environment)item, basePath, ct);

    public async Task WriteAsync(Environment item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        var path = PathResolver.EnvironmentPath(item.Project, item);
        Directory.CreateDirectory(path);

        var manifest = item.ToManifest();
        var filePath = PathResolver.EnvironmentFilePath(item.Project, item);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Environment manifest written to {FilePath}", filePath);
    }

    public async Task WriteToAsync(Environment item, string basePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));

        var dir = Path.Combine(basePath, "projects", item.Project.Name, PathResolver.EnvironmentDirectory, item.Name);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, PathResolver.EnvironmentFile);
        var yaml = _serializer.Serialize(item.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogDebug("Environment manifest written to {FilePath}", filePath);
    }

    public Task RenameAsync(Environment item, string oldName, string newName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        var oldPath = PathResolver.EnvironmentPath(item.Project.Name, oldName);
        var newPath = PathResolver.EnvironmentPath(item.Project.Name, newName);

        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);

        logger.LogInformation("Environment manifest renamed from {OldPath} to {NewPath}", oldPath, newPath);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Environment>> ReadAsync(Guid parentId = default, CancellationToken ct = default)
    {
        if (parentId == Guid.Empty) return [];

        var project = await projectRepository.GetByIdAsync(parentId, ct);
        if (project == null) return [];

        var environmentPath = PathResolver.EnvironmentPath(project.Name);
        if (!Directory.Exists(environmentPath))
        {
            logger.LogInformation("No environment manifests found for project {ProjectName} at {Path}", project.Name, environmentPath);
            return [];
        }

        var environments = new List<Environment>();
        var environmentDirs = Directory.GetDirectories(environmentPath);

        foreach (var environmentDir in environmentDirs)
        {
            var environmentName = Path.GetFileName(environmentDir);
            var filePath = PathResolver.EnvironmentFilePath(project.Name, environmentName);

            if (File.Exists(filePath))
            {
                var yaml = await File.ReadAllTextAsync(filePath, ct);
                var manifest = _deserializer.Deserialize<EnvironmentManifestDto>(yaml);

                if (manifest != null)
                {
                    var environment = manifest.ToEntity(project);
                    environments.Add(environment);
                }
            }
        }

        return environments;
    }

    public async Task<IReadOnlyList<Environment>> ReadFromAsync(string basePath, Guid parentId = default, CancellationToken ct = default)
    {
        // basePath is the snapshot root; environments live under "projects/{name}/environments/"
        var projectsPath = Path.Combine(basePath, "projects");
        if (!Directory.Exists(projectsPath))
            return [];

        var environments = new List<Environment>();

        foreach (var projectDir in Directory.EnumerateDirectories(projectsPath))
        {
            var projectFilePath = Path.Combine(projectDir, PathResolver.ProjectFile);
            if (!File.Exists(projectFilePath)) continue;

            var projectYaml = await File.ReadAllTextAsync(projectFilePath, ct);
            var projectManifest = _deserializer.Deserialize<ProjectManifestDto>(projectYaml);
            if (projectManifest is null) continue;

            if (parentId != Guid.Empty && projectManifest.Id != parentId) continue;

            var projectStub = projectManifest.FromManifest();

            var environmentsPath = Path.Combine(projectDir, PathResolver.EnvironmentDirectory);
            if (!Directory.Exists(environmentsPath)) continue;

            foreach (var environmentDir in Directory.EnumerateDirectories(environmentsPath))
            {
                var filePath = Path.Combine(environmentDir, PathResolver.EnvironmentFile);
                if (!File.Exists(filePath)) continue;

                var yaml = await File.ReadAllTextAsync(filePath, ct);
                var manifest = _deserializer.Deserialize<EnvironmentManifestDto>(yaml);
                if (manifest is null) continue;

                environments.Add(manifest.ToEntity(projectStub));
                logger.LogDebug("Read environment manifest from {FilePath}", filePath);
            }
        }

        return environments;
    }

    public Task RemoveAsync(Environment item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        var path = PathResolver.EnvironmentPath(item.Project, item);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Environment manifest removed from {Path}", path);
        return Task.CompletedTask;
    }

    public Task<string> ReadManifestAsync(Environment item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        var filePath = PathResolver.EnvironmentFilePath(item.Project, item);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Environment manifest not found at {filePath}");

        return File.ReadAllTextAsync(filePath, ct);
    }
}