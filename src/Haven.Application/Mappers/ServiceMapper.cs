using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Exporting;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services;
using Haven.Application.Features.Services.Queries;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Riok.Mapperly.Abstractions;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ServiceMapper
{
    [MapperIgnoreTarget(nameof(ServiceDto.WebhookUrl))]
    [MapperIgnoreTarget(nameof(ServiceDto.Status))]
    [MapperIgnoreTarget(nameof(ServiceDto.RuntimeState))]
    [MapperIgnoreTarget(nameof(ServiceDto.Health))]
    [MapperIgnoreTarget(nameof(ServiceDto.SourceConfig))]
    [MapperIgnoreTarget(nameof(ServiceDto.RepositoryBinding))]
    private static partial ServiceDto ToDtoPartial(this Service service);

    public static ServiceDto ToDto(this Service service, ServiceStateSnapshot? state = null)
    {
        state ??= ServiceStateSnapshot.Empty;
        var partial = service.ToDtoPartial();
        partial.Status = state.GetDisplayStatus(service.Id);
        partial.RuntimeState = state.GetRuntimeState(service.Id);
        partial.Health = state.GetHealth(service.Id);
        partial.WebhookUrl = $"/webhooks/deploy/{service.Token}";
        partial.SourceConfig = service.SourceConfig;
        partial.RepositoryBinding = RepositoryBindingDto.From(service.RepositoryBinding);

        return partial;
    }

    public static ServiceDashboardDto ToDashboardDto(this Service service, ServiceStateSnapshot? state = null)
    {
        state ??= ServiceStateSnapshot.Empty;
        return new ServiceDashboardDto
        {
            Id = service.Id,
            EnvironmentId = service.EnvironmentId,
            Name = service.Name,
            Alias = service.Alias,
            Type = service.Type,
            ExposureMode = service.ExposureMode,
            DesiredState = service.DesiredState,
            RuntimeState = state.GetRuntimeState(service.Id),
            Status = state.GetDisplayStatus(service.Id),
            Health = state.GetHealth(service.Id),
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt,
            LastDeployedAt = service.LastDeployedAt,
            SourceConfig = service.SourceConfig,
            RepositoryBinding = RepositoryBindingDto.From(service.RepositoryBinding),
            WebhookUrl = $"/webhooks/deploy/{service.Token}"
        };
    }

    public static Service ToEntity(this ServiceManifestDto dto, Environment environment)
    {
        var service = Service.Reconstitute(
            dto.Id == Guid.Empty ? Guid.CreateVersion7() : dto.Id,
            environment.Id,
            dto.Name,
            dto.Alias,
            dto.Type,
            dto.ExposureMode,
            dto.DesiredState ?? ServiceManifestDto.ParseLegacyStatus(dto.Status),
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.SourceConfig.ToDomain(dto.Type),
            environment);

        service.Token = dto.Token;
        service.FeatureFlags = dto.FeatureFlags.Select(f => f.ToEntity(service.Id)).ToList();
        service.Volumes = dto.Volumes.Select(v => v.ToEntity(service.Id)).ToList();
        service.CustomActions = dto.CustomActions.Select(a => a.ToEntity(service.Id)).ToList();
        return service;
    }

    private static partial ServiceManifestDto ToManifestPartial(this Service service);

    public static ServiceManifestDto ToManifest(this Service service)
    {
        var manifest = service.ToManifestPartial();
        manifest.SourceConfig = service.SourceConfig.ToManifest();
        manifest.FeatureFlags = service.FeatureFlags.Select(f => f.ToManifest()).ToList();
        manifest.Volumes = service.Volumes.Select(v => v.ToManifest()).ToList();
        manifest.CustomActions = service.CustomActions.Select(a => a.ToManifest()).ToList();
        return manifest;
    }

    public static ServiceExportModel ToExportModel(
        this Service service,
        IReadOnlyList<EnvironmentVariables> environmentVariables,
        string managedVolumesRootPath,
        IEnumerable<EnvironmentVariables>? secrets = null)
    {
        var model = new ServiceExportModel
        {
            Name = service.Alias ?? service.Name,
            EnvironmentVariables = environmentVariables
                .Where(e => !string.IsNullOrEmpty(e.Key))
                .GroupBy(e => e.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value),
            SecretKeys = (secrets ?? []).Select(s => s.Key).Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList(),
            Volumes = service.Volumes.Select(v => v.ToExportModel(service.Id, managedVolumesRootPath)).ToList(),
            Networks = service.ServiceNetworks
                .Where(sn => sn.Network is { Type: not NetworkType.ProjectEnvironment })
                .Select(sn => sn.Network!.Name)
                .ToList()
        };

        // Secrets win over plain variables of the same key, as they do at deploy time.
        foreach (var key in model.SecretKeys)
            model.EnvironmentVariables.Remove(key);

        switch (service.SourceConfig)
        {
            case DockerConfig docker:
                model.Image = docker.Image;
                model.ApplyDockerSettings(docker, service.ExposureMode);
                break;
            case DockerfileConfig dockerfile:
                model.ApplyDockerfileSource(dockerfile);
                model.BuildArgs = new Dictionary<string, string>(dockerfile.BuildArgs ?? []);
                model.ApplyDockerSettings(dockerfile, service.ExposureMode);
                break;
        }

        return model;
    }

    private static void ApplyDockerfileSource(this ServiceExportModel model, DockerfileConfig dockerfile)
    {
        if (dockerfile.Source == DockerfileSource.Raw)
        {
            model.DockerfileContent = string.IsNullOrWhiteSpace(dockerfile.Content) ? null : dockerfile.Content;
            return;
        }

        var subdirectory = string.IsNullOrWhiteSpace(dockerfile.BuildContext) ? null : dockerfile.BuildContext.Trim('/');

        // Compose resolves the Dockerfile relative to the build context, while Haven stores it relative to the repository.
        var filePath = string.IsNullOrWhiteSpace(dockerfile.FilePath) ? "Dockerfile" : dockerfile.FilePath;
        model.DockerfilePath = subdirectory is null
            ? filePath
            : Path.GetRelativePath(subdirectory, filePath).Replace('\\', '/');

        if (!string.IsNullOrWhiteSpace(dockerfile.Repository))
        {
            // Compose git context syntax: <url>#<ref>:<subdirectory>
            var fragment = (dockerfile.Branch, subdirectory) switch
            {
                ({ Length: > 0 } branch, not null) => $"#{branch}:{subdirectory}",
                ({ Length: > 0 } branch, null) => $"#{branch}",
                (_, not null) => $"#:{subdirectory}",
                _ => string.Empty
            };
            model.BuildContext = dockerfile.Repository + fragment;
        }
        else
        {
            model.BuildContext = subdirectory;
        }
    }

    private static void ApplyDockerSettings(this ServiceExportModel model, DockerBasedSourceConfig config, ExposureMode exposureMode)
    {
        model.Command = [.. config.CommandArgs];
        model.Restart = config.RestartPolicy switch
        {
            RestartPolicy.No => "no",
            RestartPolicy.Always => "always",
            RestartPolicy.UnlessStopped => "unless-stopped",
            RestartPolicy.OnFailure => "on-failure",
            _ => null
        };

        model.MemoryLimit = config.MemoryLimit > 0 ? config.MemoryLimit : null;
        model.MemorySwap = config.MemorySwap != 0 ? config.MemorySwap : null;
        model.MemoryReservation = config.MemoryReservation > 0 ? config.MemoryReservation : null;
        model.Cpus = config.NanoCpus > 0 ? config.NanoCpus / 1_000_000_000m : null;
        model.Ports = BuildComposePorts(config.Ports, exposureMode);
    }

    /// <summary>
    /// Mirrors how deployment binds ports: none are published when not exposed, internal services only bind to
    /// loopback, and custom mode honours an explicit <c>hostIp:hostPort:containerPort</c> mapping.
    /// </summary>
    private static List<string> BuildComposePorts(IEnumerable<string> ports, ExposureMode exposureMode)
    {
        if (exposureMode == ExposureMode.None)
            return [];

        var result = new List<string>();
        foreach (var mapping in ports)
        {
            var parts = mapping.Split(':');
            if (parts.Length < 2)
                continue;

            if (parts.Length >= 3 && exposureMode == ExposureMode.Custom)
                result.Add(mapping);
            else if (exposureMode == ExposureMode.Internal)
                result.Add($"127.0.0.1:{parts[0]}:{parts[1]}");
            else
                result.Add($"{parts[0]}:{parts[1]}");
        }

        return result;
    }

    private static ServiceExportVolumeModel ToExportModel(this ServiceVolume volume, Guid serviceId, string managedVolumesRootPath) => new()
    {
        Type = volume.Type,
        Target = volume.Target,
        ReadOnly = volume.ReadOnly,
        Source = volume.Type == VolumeType.Managed
            ? Path.GetFullPath(Path.Combine(managedVolumesRootPath, serviceId.ToString(), volume.Id.ToString()))
            : volume.Source ?? string.Empty
    };

    public static ServiceSourceConfigManifest? ToManifest(this ServiceSourceConfig? config)
    {
        return config switch
        {
            DockerConfig docker => docker.ToManifest(),
            DockerfileConfig dockerfile => dockerfile.ToManifest(),
            null => null,
            _ => throw new InvalidOperationException($"Unknown source config type: {config.GetType().Name}")
        };
    }

    private static ServiceSourceConfigManifest ToManifest(this DockerConfig docker) => new()
    {
        Type = "docker",
        Image = docker.Image,
        Ports = [.. docker.Ports],
        CommandArgs = [.. docker.CommandArgs],
        RestartPolicy = docker.RestartPolicy,
        MemoryLimit = docker.MemoryLimit,
        MemorySwap = docker.MemorySwap,
        MemoryReservation = docker.MemoryReservation,
        NanoCpus = docker.NanoCpus
    };

    private static ServiceSourceConfigManifest ToManifest(this DockerfileConfig dockerfile) => new()
    {
        Type = "dockerfile",
        DockerfileSource = dockerfile.Source,
        Repository = dockerfile.Repository,
        Branch = dockerfile.Branch,
        FilePath = dockerfile.FilePath,
        BuildContext = dockerfile.BuildContext,
        GitCredentialId = dockerfile.GitCredentialId,
        Content = dockerfile.Content,
        Ports = [.. dockerfile.Ports],
        CommandArgs = [.. dockerfile.CommandArgs],
        RestartPolicy = dockerfile.RestartPolicy,
        BuildArgs = new Dictionary<string, string>(dockerfile.BuildArgs ?? []),
        MemoryLimit = dockerfile.MemoryLimit,
        MemorySwap = dockerfile.MemorySwap,
        MemoryReservation = dockerfile.MemoryReservation,
        NanoCpus = dockerfile.NanoCpus
    };

    public static ServiceSourceConfig? ToDomain(this ServiceSourceConfigManifest? manifest, ServiceType serviceType)
    {
        var effectiveType = manifest?.Type is { Length: > 0 } t ? t : serviceType switch
        {
            ServiceType.DockerImage => "docker",
            ServiceType.Dockerfile => "dockerfile",
            _ => null
        };

        ServiceSourceConfig? config = effectiveType switch
        {
            "docker" when manifest is not null => manifest.ToDockerConfig(),
            "dockerfile" when manifest is not null => manifest.ToDockerfileConfig(),
            _ => null
        };

        return config;
    }

    public static DockerConfig ToDockerConfig(this ServiceSourceConfigManifest manifest) => new()
    {
        Image = manifest.Image ?? string.Empty,
        Ports = [.. manifest.Ports ?? []],
        CommandArgs = [.. manifest.CommandArgs ?? []],
        RestartPolicy = manifest.RestartPolicy,
        MemoryLimit = manifest.MemoryLimit,
        MemorySwap = manifest.MemorySwap,
        MemoryReservation = manifest.MemoryReservation,
        NanoCpus = manifest.NanoCpus
    };

    public static DockerfileConfig ToDockerfileConfig(this ServiceSourceConfigManifest manifest) => new()
    {
        Source = manifest.DockerfileSource ?? DockerfileSource.Raw,
        Repository = manifest.Repository,
        Branch = manifest.Branch,
        FilePath = manifest.FilePath,
        BuildContext = manifest.BuildContext,
        GitCredentialId = manifest.GitCredentialId,
        Content = manifest.Content,
        Ports = [.. manifest.Ports ?? []],
        CommandArgs = [.. manifest.CommandArgs ?? []],
        RestartPolicy = manifest.RestartPolicy,
        BuildArgs = new Dictionary<string, string>(manifest.BuildArgs ?? []),
        MemoryLimit = manifest.MemoryLimit,
        MemorySwap = manifest.MemorySwap,
        MemoryReservation = manifest.MemoryReservation,
        NanoCpus = manifest.NanoCpus
    };
}