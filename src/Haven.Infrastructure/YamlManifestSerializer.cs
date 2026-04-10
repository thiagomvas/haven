using Haven.Application.Common.Interfaces;
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

    public async Task WriteProjectAsync(Project project, CancellationToken ct)
    {
        var path = ProjectPath(project);
        Directory.CreateDirectory(path);

        var projectManifest = new
        {
            project.Name,
            project.Description
        };

        var filePath = Path.Combine(path, "project.yaml");
        await WriteYamlAsync(filePath, projectManifest, ct);

        logger.LogInformation("Project manifest written to {FilePath}", filePath);
    }

    private string ProjectPath(Project project)
    {
        return Path.Combine(_basePath, "projects", project.Name);
    }

    private async Task WriteYamlAsync(string filePath, object content, CancellationToken ct)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(content);

        await File.WriteAllTextAsync(filePath, yaml, ct);
    }
}