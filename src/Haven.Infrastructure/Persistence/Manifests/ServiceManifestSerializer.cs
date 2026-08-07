using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using YamlDotNet.Serialization;

using Environment = Haven.Domain.Aggregates.Environment;
using Service = Haven.Domain.Aggregates.Service;

namespace Haven.Infrastructure.Persistence.Manifests;

public class ServiceManifestSerializer(
    IEnvironmentRepository environmentRepository,
    IOptionsMonitor<VolumesOptions> volumesOptions,
    ILogger<ServiceManifestSerializer> logger) : IManifestSerializer<Service>, IManifestParser<ServiceManifestDto>
{
    private const string VolumesDirectory = "volumes";

    private readonly ISerializer _serializer = YamlSerializerPresets.CreateSerializer();
    private readonly IDeserializer _deserializer = YamlSerializerPresets.CreateDeserializer();

    public Type EntityType => typeof(Service);

    Task IManifestEntitySerializer.WriteToAsync(object item, string basePath, CancellationToken ct)
        => WriteToAsync((Service)item, basePath, ct);

    public async Task WriteAsync(Service item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));
        ArgumentNullException.ThrowIfNull(item.Environment.Project, nameof(item.Environment.Project));

        var path = PathResolver.ServicePath(item.Environment.Project, item.Environment, item);
        Directory.CreateDirectory(path);

        var manifest = item.ToManifest();
        var filePath = PathResolver.ServiceFilePath(item.Environment.Project, item.Environment, item);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        WriteManagedVolumeFiles(item, path);

        logger.LogInformation("Service manifest written to {FilePath}", filePath);
    }

    public async Task WriteToAsync(Service item, string basePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));
        ArgumentNullException.ThrowIfNull(item.Environment.Project, nameof(item.Environment.Project));

        var dir = Path.Combine(basePath, "projects", item.Environment.Project.Name, PathResolver.EnvironmentDirectory, item.Environment.Name, PathResolver.ServiceDirectory, item.Name);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, PathResolver.ServiceFile);
        var yaml = _serializer.Serialize(item.ToManifest());
        await File.WriteAllTextAsync(filePath, yaml, ct);

        WriteManagedVolumeFiles(item, dir);

        logger.LogDebug("Service manifest written to {FilePath}", filePath);
    }

    public Task RenameAsync(Service item, string oldName, string newName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));
        ArgumentNullException.ThrowIfNull(item.Environment.Project, nameof(item.Environment.Project));

        var oldPath = PathResolver.ServicePath(item.Environment.Project.Name, item.Environment.Name, oldName);
        var newPath = PathResolver.ServicePath(item.Environment.Project.Name, item.Environment.Name, newName);

        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);

        logger.LogInformation("Service manifest renamed from {OldPath} to {NewPath}", oldPath, newPath);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Service>> ReadAsync(Guid parentId = default, CancellationToken ct = default)
    {
        if (parentId == Guid.Empty) return [];

        var environment = await environmentRepository.GetByIdAsync(parentId, ct);
        if (environment?.Project == null) return [];

        var servicesRootPath = Path.Combine(PathResolver.EnvironmentPath(environment.Project.Name, environment.Name), "services");

        if (!Directory.Exists(servicesRootPath))
        {
            logger.LogInformation("No service manifests found for environment {EnvironmentName} at {Path}", environment.Name, servicesRootPath);
            return [];
        }

        var services = new List<Service>();
        var serviceDirs = Directory.GetDirectories(servicesRootPath);

        foreach (var serviceDir in serviceDirs)
        {
            var serviceName = Path.GetFileName(serviceDir);
            var filePath = PathResolver.ServiceFilePath(environment.Project.Name, environment.Name, serviceName);

            if (File.Exists(filePath))
            {
                var yaml = await File.ReadAllTextAsync(filePath, ct);
                var manifest = _deserializer.Deserialize<ServiceManifestDto>(yaml);

                if (manifest != null)
                {
                    var service = manifest.ToEntity(environment);
                    services.Add(service);
                }
            }
        }

        return services;
    }

    public async Task<IReadOnlyList<Service>> ReadFromAsync(string basePath, Guid parentId = default, CancellationToken ct = default)
    {
        var projectsPath = Path.Combine(basePath, "projects");
        if (!Directory.Exists(projectsPath))
            return [];

        var services = new List<Service>();

        foreach (var projectDir in Directory.EnumerateDirectories(projectsPath))
        {
            var environmentsPath = Path.Combine(projectDir, PathResolver.EnvironmentDirectory);
            if (!Directory.Exists(environmentsPath)) continue;

            foreach (var environmentDir in Directory.EnumerateDirectories(environmentsPath))
            {
                var environmentFilePath = Path.Combine(environmentDir, PathResolver.EnvironmentFile);
                if (!File.Exists(environmentFilePath)) continue;

                var environmentYaml = await File.ReadAllTextAsync(environmentFilePath, ct);
                var environmentManifest = _deserializer.Deserialize<EnvironmentManifestDto>(environmentYaml);
                if (environmentManifest is null) continue;

                var projectFilePath = Path.Combine(projectDir, PathResolver.ProjectFile);
                if (!File.Exists(projectFilePath)) continue;

                var projectYaml = await File.ReadAllTextAsync(projectFilePath, ct);
                var projectManifest = _deserializer.Deserialize<ProjectManifestDto>(projectYaml);
                if (projectManifest is null) continue;

                var projectStub = projectManifest.FromManifest();
                var environmentEntity = environmentManifest.ToEntity(projectStub);

                var servicesPath = Path.Combine(environmentDir, PathResolver.ServiceDirectory);
                if (!Directory.Exists(servicesPath)) continue;

                foreach (var serviceDir in Directory.EnumerateDirectories(servicesPath))
                {
                    var serviceFilePath = Path.Combine(serviceDir, PathResolver.ServiceFile);
                    if (!File.Exists(serviceFilePath)) continue;

                    var serviceYaml = await File.ReadAllTextAsync(serviceFilePath, ct);
                    var serviceManifest = _deserializer.Deserialize<ServiceManifestDto>(serviceYaml);
                    if (serviceManifest is null) continue;

                    var service = serviceManifest.ToEntity(environmentEntity);
                    services.Add(service);
                    logger.LogDebug("Read service manifest from {FilePath}", serviceFilePath);
                }
            }
        }

        return services;
    }

    public Task RemoveAsync(Service item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));
        ArgumentNullException.ThrowIfNull(item.Environment.Project, nameof(item.Environment.Project));

        var path = PathResolver.ServicePath(item.Environment.Project, item.Environment, item);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Service manifest removed from {Path}", path);
        return Task.CompletedTask;
    }

    public Task<string> ReadManifestAsync(Service item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item.Environment, nameof(item.Environment));
        ArgumentNullException.ThrowIfNull(item.Environment.Project, nameof(item.Environment.Project));

        var filePath = PathResolver.ServiceFilePath(item.Environment.Project, item.Environment, item);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Service manifest file not found at {filePath}");

        return File.ReadAllTextAsync(filePath, ct);
    }

    public Task<ServiceManifestDto> ParseAsync(string yaml, CancellationToken ct = default)
    {
        var manifest = _deserializer.Deserialize<ServiceManifestDto>(yaml);
        return Task.FromResult(manifest);
    }

    /// <summary>
    /// Copies the files of every backup-enabled managed volume into <c>{serviceDir}/volumes/{volumeName}/</c>.
    /// The destination <c>volumes</c> directory is rebuilt each time so removed files/volumes do not linger.
    /// </summary>
    private void WriteManagedVolumeFiles(Service item, string serviceDir)
    {
        var backedUpManaged = item.Volumes
            .Where(v => v.BackupEnabled && v.Type == VolumeType.Managed)
            .ToList();

        var volumesDir = Path.Combine(serviceDir, VolumesDirectory);
        if (Directory.Exists(volumesDir))
            Directory.Delete(volumesDir, recursive: true);

        if (backedUpManaged.Count == 0)
            return;

        var root = volumesOptions.CurrentValue.RootPath;

        foreach (var volume in backedUpManaged)
        {
            var sourceDir = DockerUtils.ManagedVolumeHostPath(root, item.Id, volume.Id);
            if (!Directory.Exists(sourceDir))
                continue;

            DirectoryUtils.CopyDirectory(sourceDir, Path.Combine(volumesDir, volume.Name));
        }
    }
}