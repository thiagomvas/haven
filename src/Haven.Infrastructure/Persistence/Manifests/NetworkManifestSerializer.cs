using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Networks;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using YamlDotNet.Serialization;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Persistence.Manifests;

public class NetworkManifestSerializer(ILogger<NetworkManifestSerializer> logger) : IManifestSerializer<Network>
{
    private readonly ISerializer _serializer = YamlSerializerPresets.CreateSerializer();
    private readonly IDeserializer _deserializer = YamlSerializerPresets.CreateDeserializer();

    public Type EntityType => typeof(Network);

    Task IManifestEntitySerializer.WriteToAsync(object item, string basePath, CancellationToken ct)
        => WriteToAsync((Network)item, basePath, ct);

    public async Task WriteAsync(Network item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));

        var path = PathResolver.EnvironmentPath(item.Project, item.Environment);
        Directory.CreateDirectory(path);

        var manifest = item.ToManifest();
        var filePath = PathResolver.NetworkFilePath(item.Project, item.Environment);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Network manifest written to {FilePath}", filePath);
    }

    public async Task WriteToAsync(Network item, string basePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));

        var dir = Path.Combine(basePath, "projects", item.Project.Name, PathResolver.EnvironmentDirectory, item.Environment.Name);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, PathResolver.NetworkFile);
        var yaml = _serializer.Serialize(item.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogDebug("Network manifest written to {FilePath}", filePath);
    }

    public Task RenameAsync(Network item, string oldName, string newName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));

        var oldPath = PathResolver.EnvironmentPath(item.Project.Name, oldName);
        var newPath = PathResolver.EnvironmentPath(item.Project.Name, newName);

        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);

        logger.LogInformation("Network manifest renamed from {OldPath} to {NewPath}", oldPath, newPath);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Network>> ReadAsync(Guid parentId = default, CancellationToken ct = default)
    {
        var networks = new List<Network>();
        var projectsPath = PathResolver.ProjectsDirectory;

        if (!Directory.Exists(projectsPath))
        {
            logger.LogInformation("No manifests directory found at {Path}, skipping network sync", projectsPath);
            return networks;
        }

        foreach (var projectDir in Directory.EnumerateDirectories(projectsPath))
        {
            var environmentsPath = Path.Combine(projectDir, PathResolver.EnvironmentDirectory);
            if (!Directory.Exists(environmentsPath))
                continue;

            foreach (var environmentDir in Directory.EnumerateDirectories(environmentsPath))
            {
                var networkFilePath = Path.Combine(environmentDir, PathResolver.NetworkFile);

                if (File.Exists(networkFilePath))
                {
                    try
                    {
                        var yaml = await File.ReadAllTextAsync(networkFilePath, ct);
                        var manifest = _deserializer.Deserialize<NetworkManifestDto>(yaml);

                        if (manifest != null)
                        {
                            var envManifestPath = Path.Combine(environmentDir, PathResolver.EnvironmentFile);
                            var envYaml = await File.ReadAllTextAsync(envManifestPath, ct);
                            var envManifest = _deserializer.Deserialize<EnvironmentManifestDto>(envYaml);

                            if (envManifest != null)
                            {
                                var network = manifest.FromManifest(envManifest.ProjectId, envManifest.Id);
                                networks.Add(network);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to read network manifest from {Path}", networkFilePath);
                    }
                }
            }
        }

        return networks;
    }

    public async Task<IReadOnlyList<Network>> ReadFromAsync(string basePath, Guid parentId = default, CancellationToken ct = default)
    {
        var projectsPath = Path.Combine(basePath, "projects");
        if (!Directory.Exists(projectsPath))
            return [];

        var networks = new List<Network>();

        foreach (var projectDir in Directory.EnumerateDirectories(projectsPath))
        {
            var environmentsPath = Path.Combine(projectDir, PathResolver.EnvironmentDirectory);
            if (!Directory.Exists(environmentsPath)) continue;

            foreach (var environmentDir in Directory.EnumerateDirectories(environmentsPath))
            {
                var networkFilePath = Path.Combine(environmentDir, PathResolver.NetworkFile);
                if (!File.Exists(networkFilePath)) continue;

                try
                {
                    var yaml = await File.ReadAllTextAsync(networkFilePath, ct);
                    var manifest = _deserializer.Deserialize<NetworkManifestDto>(yaml);
                    if (manifest is null) continue;

                    var envManifestPath = Path.Combine(environmentDir, PathResolver.EnvironmentFile);
                    var envYaml = await File.ReadAllTextAsync(envManifestPath, ct);
                    var envManifest = _deserializer.Deserialize<EnvironmentManifestDto>(envYaml);
                    if (envManifest is null) continue;

                    networks.Add(manifest.FromManifest(envManifest.ProjectId, envManifest.Id));
                    logger.LogDebug("Read network manifest from {Path}", networkFilePath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to read network manifest from {Path}", networkFilePath);
                }
            }
        }

        return networks;
    }

    public Task RemoveAsync(Network item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));

        var filePath = PathResolver.NetworkFilePath(item.Project, item.Environment);

        if (File.Exists(filePath))
            File.Delete(filePath);

        logger.LogInformation("Network manifest removed from {FilePath}", filePath);
        return Task.CompletedTask;
    }

    public Task<string> ReadManifestAsync(Network item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Project, nameof(item.Project));
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));

        var filePath = PathResolver.NetworkFilePath(item.Project, item.Environment);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Network manifest file not found at {filePath}");

        return File.ReadAllTextAsync(filePath, ct);
    }
}