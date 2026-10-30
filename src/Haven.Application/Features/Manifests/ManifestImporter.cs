using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Environment = Haven.Domain.Aggregates.Environment;
using FeatureFlag = Haven.Domain.Entities.FeatureFlag;

namespace Haven.Application.Features.Manifests;

/// <summary>
/// Materialises exported manifest DTOs as brand-new entities. Ids from the manifest are never reused,
/// so the same file can be imported repeatedly without colliding with what already exists.
/// </summary>
public sealed class ManifestImporter(
    IEnvironmentRepository environmentRepository,
    INetworkRepository networkRepository,
    IServiceRepository serviceRepository,
    IFeatureFlagRepository featureFlagRepository)
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static Result<T> Parse<T>(string yaml) where T : class
    {
        try
        {
            var manifest = Deserializer.Deserialize<T?>(yaml);
            return manifest is null
                ? Error.Validation("The manifest is empty.")
                : Result<T>.Success(manifest);
        }
        catch (YamlException ex)
        {
            return Error.Validation($"The manifest is not valid: {ex.Message}");
        }
    }

    public static Result<ProjectManifestDto> ParseProject(string yaml)
    {
        var parsed = Parse<ProjectManifestDto>(yaml);
        if (!parsed.IsSuccess) return parsed;

        var environments = parsed.Value.Environments;
        if (HasDuplicates(environments.Select(e => e.Name)))
            return Error.Validation("The manifest contains environments with duplicate names.");

        return environments.Any(e => HasDuplicateServiceNames(e))
            ? Error.Validation("The manifest contains services with duplicate names within an environment.")
            : parsed;
    }

    public static Result<EnvironmentManifestDto> ParseEnvironment(string yaml)
    {
        var parsed = Parse<EnvironmentManifestDto>(yaml);
        return parsed.IsSuccess && HasDuplicateServiceNames(parsed.Value)
            ? Error.Validation("The manifest contains services with duplicate names.")
            : parsed;
    }

    /// <summary>Adds a copy of <paramref name="source"/> to <paramref name="project"/>, plus its network and, optionally, its services.</summary>
    public async Task<Environment> ImportEnvironmentAsync(
        Project project,
        EnvironmentManifestDto source,
        string name,
        string? alias,
        bool includeServices,
        CancellationToken cancellationToken)
    {
        var environment = project.AddEnvironment(name, alias, source.Description);
        environmentRepository.AddAsync(environment, cancellationToken);

        var network = Network.CreateProjectEnvironmentNetwork(
            project.Id,
            project.Alias ?? project.Id.ToString("N")[..8],
            environment.Id,
            environment.Alias ?? environment.Id.ToString("N")[..8]);
        await networkRepository.AddAsync(network, cancellationToken);

        if (includeServices)
        {
            foreach (var serviceManifest in source.Services)
                await ImportServiceAsync(project, environment, serviceManifest, cancellationToken);
        }

        return environment;
    }

    private async Task ImportServiceAsync(
        Project project,
        Environment environment,
        ServiceManifestDto source,
        CancellationToken cancellationToken)
    {
        var service = project.AddService(
            environment.Id,
            source.Name,
            source.Type,
            source.ExposureMode,
            source.Alias,
            source.SourceConfig.ToDomain(source.Type));

        foreach (var volume in source.Volumes)
            service.AddVolume(volume.Type, volume.Name, volume.Target, volume.Source, volume.ReadOnly, volume.BackupEnabled);

        foreach (var action in source.CustomActions)
        {
            action.Id = Guid.Empty;
            service.CustomActions.Add(action.ToEntity(service.Id));
        }

        await serviceRepository.AddAsync(service, cancellationToken);

        var flags = source.FeatureFlags
            .Select(f => FeatureFlag.Create(service.Id, f.Name, f.Type, f.Key, f.Description, f.Value, f.ValueType))
            .ToList();
        if (flags.Count > 0)
            await featureFlagRepository.AddAsync(flags, cancellationToken);
    }

    private static bool HasDuplicateServiceNames(EnvironmentManifestDto environment) =>
        HasDuplicates(environment.Services.Select(s => s.Name));

    private static bool HasDuplicates(IEnumerable<string> names) =>
        names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1);
}
