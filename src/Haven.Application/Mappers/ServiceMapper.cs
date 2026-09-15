using Haven.Application.Features.Exporting;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services;
using Haven.Application.Features.Services.Queries;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;
using Haven.Domain.ValueObjects;

using Riok.Mapperly.Abstractions;

using Environment = Haven.Domain.Aggregates.Environment;

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

    public static ServiceDashboardDto ToDashboardDto(this Service service)
    {
        return new ServiceDashboardDto
        {
            Id = service.Id,
            EnvironmentId = service.EnvironmentId,
            Name = service.Name,
            Alias = service.Alias,
            Type = service.Type,
            ExposureMode = service.ExposureMode,
            Status = service.Status,
            Health = service.Health,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt,
            LastDeployedAt = service.LastDeployedAt,
            SourceConfig = service.SourceConfig,
            WebhookUrl = $"/webhooks/deploy/{service.Token}"
        };
    }

    public static Service ToEntity(this ServiceManifestDto dto, Environment environment)
    {
        var service = Service.Reconstitute(
            dto.Id,
            environment.Id,
            dto.Name,
            dto.Alias,
            dto.Type,
            dto.ExposureMode,
            dto.Status,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.SourceConfig.ToDomain(dto.Type),
            environment);

        service.Token = dto.Token;
        service.FeatureFlags = dto.FeatureFlags.Select(f => f.ToEntity(service.Id)).ToList();
        service.Volumes = dto.Volumes.Select(v => v.ToEntity(service.Id)).ToList();
        return service;
    }

    private static partial ServiceManifestDto ToManifestPartial(this Service service);

    public static ServiceManifestDto ToManifest(this Service service)
    {
        var manifest = service.ToManifestPartial();
        manifest.SourceConfig = service.SourceConfig.ToManifest();
        manifest.FeatureFlags = service.FeatureFlags.Select(f => f.ToManifest()).ToList();
        manifest.Volumes = service.Volumes.Select(v => v.ToManifest()).ToList();
        return manifest;
    }

    public static ServiceExportModel ToExportModel(this Service service, IReadOnlyList<EnvironmentVariables> environmentVariables, string managedVolumesRootPath) => new()
    {
        Name = service.Alias ?? service.Name,
        Image = (service.SourceConfig as DockerConfig)?.Image,
        DockerfilePath = (service.SourceConfig as DockerfileConfig)?.FilePath,
        EnvironmentVariables = environmentVariables.ToDictionary(e => e.Key, e => e.Value),
        Volumes = service.Volumes.Select(v => v.ToExportModel(service.Id, managedVolumesRootPath)).ToList()
    };

    private static ServiceExportVolumeModel ToExportModel(this ServiceVolume volume, Guid serviceId, string managedVolumesRootPath) => new()
    {
        Type = volume.Type,
        Target = volume.Target,
        ReadOnly = volume.ReadOnly,
        Source = volume.Type == VolumeType.Managed
            ? Path.GetFullPath(Path.Combine(managedVolumesRootPath, serviceId.ToString(), volume.Id.ToString()))
            : volume.Source ?? string.Empty
    };

    public static ServiceData ToServiceData(this ServiceManifestDto dto)
        => new(dto.Id, dto.EnvironmentId, dto.Name, dto.Alias, dto.Type, dto.ExposureMode, dto.Status, dto.CreatedAt, dto.UpdatedAt, dto.Token, dto.SourceConfig.ToDomain(dto.Type));

    private static ServiceSourceConfigManifest? ToManifest(this ServiceSourceConfig? config) => config switch
    {
        DockerConfig docker => new ServiceSourceConfigManifest
        {
            Type = "docker",
            Image = docker.Image,
            Ports = docker.Ports,
            CommandArgs = docker.CommandArgs,
            RestartPolicy = docker.RestartPolicy
        },
        DockerfileConfig dockerfile => new ServiceSourceConfigManifest
        {
            Type = "dockerfile",
            DockerfileSource = dockerfile.Source,
            Repository = dockerfile.Repository,
            Branch = dockerfile.Branch,
            FilePath = dockerfile.FilePath,
            GitCredentialId = dockerfile.GitCredentialId,
            Content = dockerfile.Content,
            Ports = dockerfile.Ports,
            CommandArgs = dockerfile.CommandArgs,
            RestartPolicy = dockerfile.RestartPolicy
        },
        null => null,
        _ => throw new InvalidOperationException($"Unknown source config type: {config.GetType().Name}")
    };

    private static ServiceSourceConfig? ToDomain(this ServiceSourceConfigManifest? manifest, ServiceType serviceType)
    {
        var effectiveType = manifest?.Type is { Length: > 0 } t ? t : serviceType switch
        {
            ServiceType.DockerImage => "docker",
            ServiceType.Dockerfile => "dockerfile",
            _ => null
        };

        return effectiveType switch
        {
            "docker" when manifest is not null => new DockerConfig
            {
                Image = manifest.Image ?? string.Empty,
                Ports = manifest.Ports,
                CommandArgs = manifest.CommandArgs,
                RestartPolicy = manifest.RestartPolicy
            },
            "dockerfile" when manifest is not null => new DockerfileConfig
            {
                Source = manifest.DockerfileSource ?? DockerfileSource.Raw,
                Repository = manifest.Repository,
                Branch = manifest.Branch,
                FilePath = manifest.FilePath,
                GitCredentialId = manifest.GitCredentialId,
                Content = manifest.Content,
                Ports = manifest.Ports,
                CommandArgs = manifest.CommandArgs,
                RestartPolicy = manifest.RestartPolicy
            },
            _ => null
        };
    }
}