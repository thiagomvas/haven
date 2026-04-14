using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Projects;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
            projects.Add(manifest.FromManifest());

            logger.LogInformation("Read project manifest from {FilePath}", filePath);
        }

        return projects;
    }

    private string ProjectPath(Project project) =>
        Path.Combine(_basePath, "projects", project.Name);
}