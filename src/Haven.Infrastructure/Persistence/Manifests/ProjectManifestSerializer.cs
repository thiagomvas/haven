using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Projects;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace Haven.Infrastructure.Persistence.Manifests;

public class ProjectManifestSerializer(ILogger<ProjectManifestSerializer> logger) : IManifestSerializer<Project>
{
    private readonly ISerializer _serializer = YamlSerializerPresets.CreateSerializer();
    private readonly IDeserializer _deserializer = YamlSerializerPresets.CreateDeserializer();
    public async Task WriteAsync(Project item, CancellationToken ct = default)
    {
        var path = PathResolver.ProjectPath(item);
        Directory.CreateDirectory(path);

        var manifest = item.ToManifest();
        var filePath = PathResolver.ProjectFilePath(item);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);
        
        logger.LogInformation("Project manifest written to {FilePath}", filePath);
    }

    public Task RenameAsync(Project item, string oldName, string newName, CancellationToken ct = default)
    {
        var oldPath = PathResolver.ProjectPath(oldName);
        var newPath = PathResolver.ProjectPath(newName);

        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);

        logger.LogInformation("Project manifest renamed from {OldPath} to {NewPath}", oldPath, newPath);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Project>> ReadAsync(Guid parentId = default, CancellationToken ct = default)
    {
        var projectsPath = PathResolver.ProjectsDirectory;
        if (!Directory.Exists(projectsPath))
        {
            logger.LogInformation("No manifests directory found at {Path}, skipping sync", projectsPath);
            return [];
        }
        
        var projects = new List<Project>();
        
        foreach (var dir in Directory.EnumerateDirectories(projectsPath))
        {
            var filePath = PathResolver.ProjectFilePathForDirectory(dir);
            if (!File.Exists(filePath)) continue;

            var yaml = await File.ReadAllTextAsync(filePath, ct);
            var manifest = _deserializer.Deserialize<ProjectManifestDto>(yaml);

            projects.Add(manifest.FromManifest());

            logger.LogDebug("Read project manifest from {FilePath}", filePath);
        }

        return projects;
    }

    public Task RemoveAsync(Project item, CancellationToken ct = default)
    {
        var path = PathResolver.ProjectPath(item);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Project manifest deleted at {Path}", path);
        return Task.CompletedTask;
    }
}