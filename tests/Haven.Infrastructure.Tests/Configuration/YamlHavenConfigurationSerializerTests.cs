using Haven.Application.Configuration;
using Haven.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Haven.Infrastructure.Tests.Configuration;

[Category("Unit")]
public sealed class YamlHavenConfigurationSerializerTests
{
    private string _tempDir = null!;
    private string _originalDir = null!;
    private YamlHavenConfigurationSerializer _sut = null!;

    private const string ConfigPath = "manifests/haven.yml";

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);

        var logger = Substitute.For<ILogger<YamlHavenConfigurationSerializer>>();
        _sut = new YamlHavenConfigurationSerializer(logger);
    }

    [TearDown]
    public void Cleanup()
    {
        Directory.SetCurrentDirectory(_originalDir);
        Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public async Task ReadAsync_ShouldReturnDefaults_AndCreateFile_WhenConfigFileDoesNotExist()
    {
        var result = await _sut.ReadAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result.Manifests.ManifestsPath.ShouldBe("manifests");
        result.Manifests.AutoSyncEnabled.ShouldBeTrue();
        File.Exists(ConfigPath).ShouldBeTrue();
    }

    [Test]
    public async Task ReadAsync_ShouldDeserializeConfig_WhenFileExistsWithValidYaml()
    {
        var expected = new HavenConfiguration
        {
            Manifests = new ManifestsOptions
            {
                ManifestsPath = "custom/path",
                AutoSyncEnabled = false,
                SyncIntervalSeconds = 120,
                IncludeEnvValuesOnExample = false
            }
        };
        await _sut.WriteAsync(expected, CancellationToken.None);

        var result = await _sut.ReadAsync(CancellationToken.None);

        result.Manifests.ManifestsPath.ShouldBe("custom/path");
        result.Manifests.AutoSyncEnabled.ShouldBeFalse();
        result.Manifests.SyncIntervalSeconds.ShouldBe(120);
        result.Manifests.IncludeEnvValuesOnExample.ShouldBeFalse();
    }

    [Test]
    public async Task ReadAsync_ShouldReturnDefaults_WhenFileContainsInvalidYaml()
    {
        Directory.CreateDirectory("manifests");
        await File.WriteAllTextAsync(ConfigPath, ":\tinvalid: yaml: {{{{");

        var result = await _sut.ReadAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result.Manifests.ShouldNotBeNull();
    }

    [Test]
    public async Task WriteAsync_ShouldCreateFile_WithSerializedYaml()
    {
        var config = new HavenConfiguration
        {
            Manifests = new ManifestsOptions { SyncIntervalSeconds = 30 }
        };

        await _sut.WriteAsync(config, CancellationToken.None);

        File.Exists(ConfigPath).ShouldBeTrue();
        var yaml = await File.ReadAllTextAsync(ConfigPath);
        yaml.ShouldContain("syncIntervalSeconds: 30");
    }

    [Test]
    public async Task WriteAsync_ShouldCreateManifestsDirectory_WhenItDoesNotExist()
    {
        Directory.Exists("manifests").ShouldBeFalse();

        await _sut.WriteAsync(new HavenConfiguration(), CancellationToken.None);

        Directory.Exists("manifests").ShouldBeTrue();
    }

    [Test]
    public async Task WriteAsync_ShouldUseCamelCaseNaming_InOutputYaml()
    {
        await _sut.WriteAsync(new HavenConfiguration(), CancellationToken.None);

        var yaml = await File.ReadAllTextAsync(ConfigPath);
        yaml.ShouldContain("manifests:");
        yaml.ShouldContain("autoSyncEnabled:");
        yaml.ShouldContain("syncIntervalSeconds:");
    }

    [Test]
    public async Task RoundTrip_ShouldPreserveAllProperties()
    {
        var original = new HavenConfiguration
        {
            Manifests = new ManifestsOptions
            {
                ManifestsPath = "my/manifests",
                AutoSyncEnabled = false,
                SyncIntervalSeconds = 300,
                IncludeEnvValuesOnExample = false
            }
        };

        await _sut.WriteAsync(original, CancellationToken.None);
        var restored = await _sut.ReadAsync(CancellationToken.None);

        restored.Manifests.ManifestsPath.ShouldBe(original.Manifests.ManifestsPath);
        restored.Manifests.AutoSyncEnabled.ShouldBe(original.Manifests.AutoSyncEnabled);
        restored.Manifests.SyncIntervalSeconds.ShouldBe(original.Manifests.SyncIntervalSeconds);
        restored.Manifests.IncludeEnvValuesOnExample.ShouldBe(original.Manifests.IncludeEnvValuesOnExample);
    }
}
