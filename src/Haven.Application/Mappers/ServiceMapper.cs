using Haven.Application.Features.Services;
using Haven.Application.Features.Services.Queries;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Models;
using Haven.Domain.ValueObjects;
using Riok.Mapperly.Abstractions;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ServiceMapper
{
    [MapperIgnoreTarget(nameof(ServiceDto.WebhookUrl))]
    [MapperIgnoreTarget(nameof(ServiceDto.SourceConfig))]
    private static partial ServiceDto ToDtoPartial(this Service service);

    public static ServiceDto ToDto(this Service service)
    {
        var partial = service.ToDtoPartial();
        partial.WebhookUrl = $"/webhooks/deploy/{service.Token}";
        partial.SourceConfig = service.SourceConfig;

        return partial;
    }

    public static Service ToEntity(this ServiceManifestDto dto, Environment environment)
    {
        var service = Service.Reconstitute(
            dto.Id,
            environment.Id,
            dto.Name,
            dto.Type,
            dto.ExposureMode,
            dto.Status,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.SourceConfig.ToDomain(dto.Type),
            environment);

        service.Token = dto.Token;
        return service;
    }

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
        UpdatedAt = service.UpdatedAt,
        Token = service.Token
    };

    public static ServiceData ToServiceData(this ServiceManifestDto dto)
        => new(dto.Id, dto.EnvironmentId, dto.Name, dto.Type, dto.ExposureMode, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.Token, dto.SourceConfig.ToDomain(dto.Type));

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
