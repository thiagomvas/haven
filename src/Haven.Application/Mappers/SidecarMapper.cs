using Haven.Application.Features.Services;
using Haven.Application.Features.Sidecars;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class SidecarMapper
{
    [MapperIgnoreSource(nameof(Sidecar.SidecarNetworks))]
    [MapperIgnoreSource(nameof(Sidecar.Status))]
    [MapperIgnoreSource(nameof(Sidecar.Health))]
    [MapperIgnoreSource(nameof(Sidecar.Enabled))]
    [MapperIgnoreSource(nameof(Sidecar.CreatedAt))]
    [MapperIgnoreSource(nameof(Sidecar.UpdatedAt))]
    [MapperIgnoreSource(nameof(Sidecar.LastDeployedAt))]
    [MapperIgnoreSource(nameof(Sidecar.SourceConfigJson))]
    [MapperIgnoreTarget(nameof(SidecarManifestDto.SourceConfig))]
    private static partial SidecarManifestDto ToManifestPartial(this Sidecar sidecar);

    public static SidecarManifestDto ToManifest(this Sidecar sidecar)
    {
        var manifest = sidecar.ToManifestPartial();
        return manifest with { SourceConfig = sidecar.SourceConfig.ToManifest() };
    }

    // Sidecars are keyed by Kind on disk, not Id - the manifest carries no Id, so a placeholder is
    // generated here. Callers matching this against an existing DB row must key off Kind, not Id.
    public static Sidecar FromManifest(this SidecarManifestDto dto)
    {
        var now = DateTime.UtcNow;
        return Sidecar.Reconstitute(
            Guid.NewGuid(),
            dto.Name,
            dto.Alias,
            Enum.Parse<SidecarKind>(dto.Kind),
            ServiceStatus.Stopped,
            ServiceHealth.Unknown,
            enabled: false,
            createdAt: now,
            updatedAt: now,
            sourceConfig: dto.SourceConfig.ToDomain());
    }

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

    public static ServiceSourceConfig? ToDomain(this ServiceSourceConfigManifest? manifest) => manifest?.Type switch
    {
        "docker" => new DockerConfig
        {
            Image = manifest.Image ?? string.Empty,
            Ports = manifest.Ports,
            CommandArgs = manifest.CommandArgs,
            RestartPolicy = manifest.RestartPolicy
        },
        "dockerfile" => new DockerfileConfig
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