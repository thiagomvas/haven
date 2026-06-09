using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Networks;
using Haven.Application.Features.Projects;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Models;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Infrastructure;

public sealed class YamlManifestSerializer(
    ILogger<YamlManifestSerializer> logger
) : IManifestSerializer
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public async Task WriteProjectAsync(Project project, CancellationToken ct)
    {
        var path = PathResolver.ProjectPath(project);
        Directory.CreateDirectory(path);

        var manifest = project.ToManifest();
        var filePath = PathResolver.ProjectFilePath(project);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Project manifest written to {FilePath}", filePath);
    }

    public Task DeleteProjectAsync(Project project, CancellationToken ct)
    {
        var path = PathResolver.ProjectPath(project);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Project manifest deleted at {Path}", path);
        return Task.CompletedTask;
    }

    public Task RenameProjectAsync(string oldProjectName, string newProjectName, CancellationToken ct)
    {
        var oldPath = PathResolver.ProjectPath(oldProjectName);
        var newPath = PathResolver.ProjectPath(newProjectName);

        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);

        logger.LogInformation("Project manifest renamed from {OldPath} to {NewPath}", oldPath, newPath);
        return Task.CompletedTask;
    }

    public async Task WriteEnvironmentAsync(Project project, Environment environment, CancellationToken ct)
    {
        var path = PathResolver.EnvironmentPath(project, environment);
        Directory.CreateDirectory(path);

        var manifest = environment.ToManifest();
        var filePath = PathResolver.EnvironmentFilePath(project, environment);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Environment manifest written to {FilePath}", filePath);
    }


    public Task DeleteEnvironmentAsync(Project project, string environmentName, CancellationToken ct)
    {
        var path = Path.Combine(PathResolver.ProjectPath(project), "environments", environmentName);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Environment manifest deleted at {Path}", path);
        return Task.CompletedTask;
    }

    public Task RenameEnvironmentAsync(Project project, string oldEnvironmentName, string newEnvironmentName,
        CancellationToken ct)
    {
        var projectPath = PathResolver.ProjectPath(project);
        var oldPath = PathResolver.EnvironmentPath(project.Name, oldEnvironmentName);
        var newPath = PathResolver.EnvironmentPath(project.Name, newEnvironmentName);

        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);

        logger.LogInformation("Environment manifest renamed from {OldPath} to {NewPath}", oldPath, newPath);
        return Task.CompletedTask;
    }

    public async Task WriteServiceAsync(Project project, Environment environment, Service service, CancellationToken ct)
    {
        var path = PathResolver.ServicePath(project, environment, service);
        Directory.CreateDirectory(path);

        var manifest = service.ToManifest();
        var filePath = PathResolver.ServiceFilePath(project, environment, service);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Service manifest written to {FilePath}", filePath);
    }

    public Task DeleteServiceAsync(Project project, Environment environment, string serviceName, CancellationToken ct)
    {
        var path = PathResolver.ServicePath(project.Name, environment.Name, serviceName);

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        logger.LogInformation("Service manifest deleted at {Path}", path);
        return Task.CompletedTask;
    }

    public Task RenameServiceAsync(Project project, Environment environment, string oldServiceName,
        string newServiceName, CancellationToken ct)
    {
        var oldPath = PathResolver.ServicePath(project.Name, environment.Name, oldServiceName);
        var newPath = PathResolver.ServicePath(project.Name, environment.Name, newServiceName);

        if (Directory.Exists(oldPath))
            Directory.Move(oldPath, newPath);

        logger.LogInformation("Service manifest renamed from {OldPath} to {NewPath}", oldPath, newPath);
        return Task.CompletedTask;
    }


    public async Task WriteNetworkAsync(Project project, Environment environment,
        Haven.Domain.Aggregates.Network network, CancellationToken ct)
    {
        var envPath = PathResolver.EnvironmentPath(project, environment);
        Directory.CreateDirectory(envPath);

        var manifest = network.ToManifest();
        var filePath = PathResolver.NetworkFilePath(project, environment);

        var yaml = _serializer.Serialize(manifest);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Network manifest written to {FilePath}", filePath);
    }

    public Task DeleteNetworkAsync(Project project, Environment environment, CancellationToken ct)
    {
        var filePath = PathResolver.NetworkFilePath(project, environment);

        if (File.Exists(filePath))
            File.Delete(filePath);

        logger.LogInformation("Network manifest deleted at {Path}", filePath);
        return Task.CompletedTask;
    }
}