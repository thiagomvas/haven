using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using YamlDotNet.Serialization;

using Environment = Haven.Domain.Entities.Environment;
using Service = Haven.Domain.Entities.Service;

namespace Haven.Infrastructure.Persistence.Manifests;

public class ServiceManifestSerializer(IEnvironmentRepository environmentRepository, ILogger<ServiceManifestSerializer> logger) : IManifestSerializer<Service>
{
    private readonly ISerializer _serializer = YamlSerializerPresets.CreateSerializer();
    private readonly IDeserializer _deserializer = YamlSerializerPresets.CreateDeserializer();

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

        logger.LogInformation("Service manifest written to {FilePath}", filePath);
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
}