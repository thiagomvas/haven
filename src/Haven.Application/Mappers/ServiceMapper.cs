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
        => new(dto.Id, dto.EnvironmentId, dto.Name, dto.Type, dto.ExposureMode, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.SourceConfig.ToDomain(dto.Type));

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

    private static ServiceSourceConfig? ToDomain(this ServiceSourceConfigManifest? manifest, ServiceType serviceType)
    {
        var effectiveType = manifest?.Type is { Length: > 0 } t ? t : serviceType switch
        {
            ServiceType.DockerImage => "docker",
            _ => null
        };

        return effectiveType switch
        {
            "docker" when manifest is not null => new DockerConfig
            {
                Image = manifest.Image ?? string.Empty,
                Ports = manifest.Ports,
                Volumes = manifest.Volumes,
                EnvironmentVariables = manifest.EnvironmentVariables,
                RestartPolicy = manifest.RestartPolicy
            },
            _ => null
        };
    }
}
