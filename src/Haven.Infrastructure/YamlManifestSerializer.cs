using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Infrastructure;

public sealed class YamlManifestSerializer(
    ILogger<YamlManifestSerializer> logger
    ) : IManifestSerializer
{
    private readonly string _basePath = "manifests";

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public async Task WriteProjectAsync(Project project, CancellationToken ct)
    {
        var path = ProjectPath(project);
        Directory.CreateDirectory(path);

        var manifest = project.ToManifest();
        var filePath = Path.Combine(path, "project.yaml");

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Project manifest written to {FilePath}", filePath);
    }

    public Task DeleteProjectAsync(Project project, CancellationToken ct)
    {
        var path = ProjectPath(project);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Project manifest deleted at {Path}", path);
        return Task.CompletedTask;
    }

    public async Task WriteEnvironmentAsync(Project project, Environment environment, CancellationToken ct)
    {
        var path = EnvironmentPath(project, environment);
        Directory.CreateDirectory(path);

        var manifest = environment.ToManifest();
        var filePath = Path.Combine(path, "environment.yaml");

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Environment manifest written to {FilePath}", filePath);
    }

    public async Task<IReadOnlyList<Project>> ReadProjectsAsync(CancellationToken ct)
    {
        var projectsPath = Path.Combine(_basePath, "projects");

        if (!Directory.Exists(projectsPath))
        {
            logger.LogInformation("No manifests directory found at {Path}, skipping sync", projectsPath);
            return [];
        }

        var projects = new List<Project>();

        foreach (var dir in Directory.EnumerateDirectories(projectsPath))
        {
            var filePath = Path.Combine(dir, "project.yaml");
            if (!File.Exists(filePath)) continue;

            var yaml = await File.ReadAllTextAsync(filePath, ct);
            var manifest = _deserializer.Deserialize<ProjectManifestDto>(yaml);

            var environments = await ReadEnvironmentsAsync(dir, ct);
            projects.Add(manifest.FromManifest(environments));

            logger.LogInformation("Read project manifest from {FilePath}", filePath);
        }

        return projects;
    }

    private async Task<List<EnvironmentData>> ReadEnvironmentsAsync(string projectDir, CancellationToken ct)
    {
        var environmentsPath = Path.Combine(projectDir, "environments");
        if (!Directory.Exists(environmentsPath))
            return [];

        var environments = new List<EnvironmentData>();

        foreach (var dir in Directory.EnumerateDirectories(environmentsPath))
        {
            var filePath = Path.Combine(dir, "environment.yaml");
            if (!File.Exists(filePath)) continue;

            var yaml = await File.ReadAllTextAsync(filePath, ct);
            var manifest = _deserializer.Deserialize<EnvironmentManifestDto>(yaml);
            var services = await ReadServicesAsync(dir, ct);
            environments.Add(manifest.ToEnvironmentData(services));

            logger.LogInformation("Read environment manifest from {FilePath}", filePath);
        }

        return environments;
    }

    public Task DeleteEnvironmentAsync(Project project, string environmentName, CancellationToken ct)
    {
        var path = Path.Combine(ProjectPath(project), "environments", environmentName);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Environment manifest deleted at {Path}", path);
        return Task.CompletedTask;
    }

    public async Task WriteServiceAsync(Project project, Environment environment, Service service, CancellationToken ct)
    {
        var path = ServicePath(project, environment, service);
        Directory.CreateDirectory(path);

        var manifest = service.ToManifest();
        var filePath = Path.Combine(path, "service.yaml");

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Service manifest written to {FilePath}", filePath);
    }

    public Task DeleteServiceAsync(Project project, Environment environment, string serviceName, CancellationToken ct)
    {
        var path = Path.Combine(EnvironmentPath(project, environment), "services", serviceName);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Service manifest deleted at {Path}", path);
        return Task.CompletedTask;
    }

    private async Task<List<ServiceData>> ReadServicesAsync(string environmentDir, CancellationToken ct)
    {
        var servicesPath = Path.Combine(environmentDir, "services");
        if (!Directory.Exists(servicesPath))
            return [];

        var services = new List<ServiceData>();

        foreach (var dir in Directory.EnumerateDirectories(servicesPath))
        {
            var filePath = Path.Combine(dir, "service.yaml");
            if (!File.Exists(filePath)) continue;

            var yaml = await File.ReadAllTextAsync(filePath, ct);
            var manifest = _deserializer.Deserialize<ServiceManifestDto>(yaml);
            services.Add(manifest.ToServiceData());

            logger.LogInformation("Read service manifest from {FilePath}", filePath);
        }

        return services;
    }

    private string ProjectPath(Project project) =>
        Path.Combine(_basePath, "projects", project.Name);

    private string EnvironmentPath(Project project, Environment environment) =>
        Path.Combine(ProjectPath(project), "environments", environment.Name);

    private string ServicePath(Project project, Environment environment, Service service) =>
        Path.Combine(EnvironmentPath(project, environment), "services", service.Name);
}