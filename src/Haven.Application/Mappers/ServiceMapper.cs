using Haven.Application.Features.Services;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Mappers;

public static class ServiceMapper
{
    public static ServiceManifestDto ToManifest(this Service service) => new()
    {
        Id = service.Id,
        EnvironmentId = service.EnvironmentId,
        Name = service.Name,
        Type = service.Type,
        ExposureMode = service.ExposureMode,
        Status = service.Status,
        SourceConfig = service.SourceConfig.ToManifest(),
        CreatedAt = service.CreatedAt,
        UpdatedAt = service.UpdatedAt
    };

    public static Project.ServiceData ToServiceData(this ServiceManifestDto dto)
        => new(dto.Id, dto.EnvironmentId, dto.Name, dto.Type, dto.ExposureMode, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.SourceConfig.ToDomain());

    private static ServiceSourceConfigManifest? ToManifest(this ServiceSourceConfig? config) => config switch
    {
        DockerConfig docker => new ServiceSourceConfigManifest
        {
            Type = "docker",
            Image = docker.Image,
            Ports = docker.Ports,
            Volumes = docker.Volumes,
            EnvironmentVariables = docker.EnvironmentVariables,
            RestartPolicy = docker.RestartPolicy
        },
        null => null,
        _ => throw new InvalidOperationException($"Unknown source config type: {config.GetType().Name}")
    };

    private static ServiceSourceConfig? ToDomain(this ServiceSourceConfigManifest? manifest) => manifest?.Type switch
    {
        "docker" => new DockerConfig
        {
            Image = manifest.Image ?? string.Empty,
            Ports = manifest.Ports,
            Volumes = manifest.Volumes,
            EnvironmentVariables = manifest.EnvironmentVariables,
            RestartPolicy = manifest.RestartPolicy
        },
        null or "" => null,
        _ => throw new InvalidOperationException($"Unknown source config type: {manifest.Type}")
    };
}
