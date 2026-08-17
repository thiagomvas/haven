using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Sidecars;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Haven.Infrastructure.Persistence.Manifests;

public class SidecarManifestSerializer(ILogger<SidecarManifestSerializer> logger)
    : IManifestSerializer<Sidecar>, IManifestParser<SidecarManifestDto>
{
    // Sidecar manifests skip a lot of the union-of-all-source-config-kinds fields that don't apply
    // to the sidecar's actual kind (e.g. dockerfile-only fields on a docker-sourced sidecar), so
    // null/empty fields are omitted here rather than using the shared preset used by other entities.
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    private readonly IDeserializer _deserializer = YamlSerializerPresets.CreateDeserializer();

    public Type EntityType => typeof(Sidecar);

    Task IManifestEntitySerializer.WriteToAsync(object item, string basePath, CancellationToken ct)
        => WriteToAsync((Sidecar)item, basePath, ct);

    public async Task WriteAsync(Sidecar item, CancellationToken ct = default)
    {
        Directory.CreateDirectory(PathResolver.SidecarsDirectoryPath);

        var filePath = PathResolver.SidecarFilePath(item.Kind);
        var yaml = _serializer.Serialize(item.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Sidecar manifest written to {FilePath}", filePath);
    }

    public async Task WriteToAsync(Sidecar item, string basePath, CancellationToken ct = default)
    {
        var sidecarsDir = Path.Combine(basePath, PathResolver.SidecarsDirectory);
        Directory.CreateDirectory(sidecarsDir);

        var filePath = PathResolver.SidecarFilePath(basePath, item.Kind);
        var yaml = _serializer.Serialize(item.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogDebug("Sidecar manifest written to {FilePath}", filePath);
    }

    public Task RenameAsync(Sidecar item, string oldName, string newName, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<Sidecar>> ReadAsync(Guid parentId = default, CancellationToken ct = default)
        => ReadSidecarsAsync(PathResolver.SidecarsDirectoryPath, ct);

    public Task<IReadOnlyList<Sidecar>> ReadFromAsync(string basePath, Guid parentId = default, CancellationToken ct = default)
        => ReadSidecarsAsync(Path.Combine(basePath, PathResolver.SidecarsDirectory), ct);

    private async Task<IReadOnlyList<Sidecar>> ReadSidecarsAsync(string sidecarsDirectory, CancellationToken ct)
    {
        if (!Directory.Exists(sidecarsDirectory))
            return [];

        var sidecars = new List<Sidecar>();

        foreach (var filePath in Directory.EnumerateFiles(sidecarsDirectory, "*.yaml"))
        {
            try
            {
                var yaml = await File.ReadAllTextAsync(filePath, ct);
                var manifest = _deserializer.Deserialize<SidecarManifestDto>(yaml);
                if (manifest is null) continue;

                sidecars.Add(manifest.FromManifest());
                logger.LogDebug("Read sidecar manifest from {Path}", filePath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read sidecar manifest from {Path}", filePath);
            }
        }

        return sidecars;
    }

    public Task RemoveAsync(Sidecar item, CancellationToken ct = default)
    {
        var filePath = PathResolver.SidecarFilePath(item.Kind);

        if (File.Exists(filePath))
            File.Delete(filePath);

        logger.LogInformation("Sidecar manifest removed from {FilePath}", filePath);
        return Task.CompletedTask;
    }

    public Task<string> ReadManifestAsync(Sidecar item, CancellationToken ct = default)
    {
        var filePath = PathResolver.SidecarFilePath(item.Kind);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Sidecar manifest file not found at {filePath}");

        return File.ReadAllTextAsync(filePath, ct);
    }

    public Task<SidecarManifestDto> ParseAsync(string yaml, CancellationToken ct = default)
    {
        var manifest = _deserializer.Deserialize<SidecarManifestDto>(yaml);
        return Task.FromResult(manifest);
    }
}