using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Networks;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
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
        if (item.Type != NetworkType.ProjectEnvironment)
        {
            Directory.CreateDirectory(PathResolver.NetworksDirectoryPath);

            var sharedFilePath = PathResolver.SharedNetworkFilePath(item.Id);
            var sharedYaml = _serializer.Serialize(item.ToManifest());
            await File.WriteAllTextAsync(sharedFilePath, sharedYaml, ct);

            logger.LogInformation("Network manifest written to {FilePath}", sharedFilePath);
            return;
        }

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
        if (item.Type != NetworkType.ProjectEnvironment)
        {
            var networksDir = Path.Combine(basePath, PathResolver.NetworksDirectory);
            Directory.CreateDirectory(networksDir);

            var sharedFilePath = PathResolver.SharedNetworkFilePath(basePath, item.Id);
            var sharedYaml = _serializer.Serialize(item.ToManifest());
            await File.WriteAllTextAsync(sharedFilePath, sharedYaml, ct);

            logger.LogDebug("Network manifest written to {FilePath}", sharedFilePath);
            return;
        }

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
        if (item.Type != NetworkType.ProjectEnvironment)
            return Task.CompletedTask;

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

        if (Directory.Exists(projectsPath))
        {
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
        }
        else
        {
            logger.LogInformation("No manifests directory found at {Path}, skipping project/environment network sync", projectsPath);
        }

        networks.AddRange(await ReadSharedNetworksAsync(PathResolver.NetworksDirectoryPath, ct));

        return networks;
    }

    public async Task<IReadOnlyList<Network>> ReadFromAsync(string basePath, Guid parentId = default, CancellationToken ct = default)
    {
        var networks = new List<Network>();
        var projectsPath = Path.Combine(basePath, "projects");

        if (Directory.Exists(projectsPath))
        {
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
        }

        networks.AddRange(await ReadSharedNetworksAsync(Path.Combine(basePath, PathResolver.NetworksDirectory), ct));

        return networks;
    }

    private async Task<IReadOnlyList<Network>> ReadSharedNetworksAsync(string networksDirectory, CancellationToken ct)
    {
        if (!Directory.Exists(networksDirectory))
            return [];

        var networks = new List<Network>();

        foreach (var filePath in Directory.EnumerateFiles(networksDirectory, "*.yaml"))
        {
            try
            {
                var yaml = await File.ReadAllTextAsync(filePath, ct);
                var manifest = _deserializer.Deserialize<NetworkManifestDto>(yaml);
                if (manifest is null) continue;

                networks.Add(manifest.FromManifest());
                logger.LogDebug("Read shared network manifest from {Path}", filePath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read shared network manifest from {Path}", filePath);
            }
        }

        return networks;
    }

    public Task RemoveAsync(Network item, CancellationToken ct = default)
    {
        var filePath = item.Type != NetworkType.ProjectEnvironment
            ? PathResolver.SharedNetworkFilePath(item.Id)
            : GetProjectEnvironmentFilePath(item);

        if (filePath is not null && File.Exists(filePath))
            File.Delete(filePath);

        logger.LogInformation("Network manifest removed from {FilePath}", filePath);
        return Task.CompletedTask;
    }

    public Task<string> ReadManifestAsync(Network item, CancellationToken ct = default)
    {
        var filePath = item.Type != NetworkType.ProjectEnvironment
            ? PathResolver.SharedNetworkFilePath(item.Id)
            : GetProjectEnvironmentFilePath(item);

        if (filePath is null || !File.Exists(filePath))
            throw new FileNotFoundException($"Network manifest file not found at {filePath}");

        return File.ReadAllTextAsync(filePath, ct);
    }

    private static string? GetProjectEnvironmentFilePath(Network item)
    {
        if (item.Project is null || item.Environment is null)
            return null;

        return PathResolver.NetworkFilePath(item.Project, item.Environment);
    }
}
