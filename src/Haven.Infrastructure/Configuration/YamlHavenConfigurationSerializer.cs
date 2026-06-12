namespace Haven.Infrastructure.Configuration;

using Application.Common.Interfaces;
using Application.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public sealed class YamlHavenConfigurationSerializer(
    ILogger<YamlHavenConfigurationSerializer> logger,
    IOptionsMonitor<ManifestsOptions> manifestsOptions) : IHavenConfigurationSerializer
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private const string ConfigFileName = "haven.yml";

    private string ManifestsDir => manifestsOptions.CurrentValue.ManifestsPath;

    public bool FileExists() => File.Exists(Path.Combine(ManifestsDir, ConfigFileName));

    public async Task<HavenConfiguration> ReadAsync(CancellationToken ct)
    {
        var filePath = Path.Combine(ManifestsDir, ConfigFileName);

        if (!File.Exists(filePath))
        {
            logger.LogInformation("Configuration file {FilePath} not found, writing defaults", filePath);
            var defaults = new HavenConfiguration();
            await WriteAsync(defaults, ct);
            return defaults;
        }

        try
        {
            var yaml = await File.ReadAllTextAsync(filePath, ct);
            var config = _deserializer.Deserialize<HavenConfiguration>(yaml) ?? new HavenConfiguration();
            logger.LogInformation("Configuration loaded from {FilePath}", filePath);
            return config;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading configuration from {FilePath}, returning defaults", filePath);
            return new HavenConfiguration();
        }
    }

    public async Task WriteAsync(HavenConfiguration config, CancellationToken ct)
    {
        var dirPath = ManifestsDir;
        var filePath = Path.Combine(dirPath, ConfigFileName);

        Directory.CreateDirectory(dirPath);

        var yaml = _serializer.Serialize(config);
        await File.WriteAllTextAsync(filePath, yaml, ct);

        logger.LogInformation("Configuration written to {FilePath}", filePath);
    }
}