using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments;
using Haven.Application.Mappers;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using YamlDotNet.Serialization;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Persistence.Manifests;

public class EnvironmentManifestSerializer(IProjectRepository projectRepository, ILogger<EnvironmentManifestSerializer> logger) : IManifestSerializer<Environment>
{
    private readonly ISerializer _serializer = YamlSerializerPresets.CreateSerializer();
    private readonly IDeserializer _deserializer = YamlSerializerPresets.CreateDeserializer();
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

    public Task RemoveAsync(Environment item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        var path = PathResolver.EnvironmentPath(item.Project, item);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Environment manifest removed from {Path}", path);
        return Task.CompletedTask;
    }
}