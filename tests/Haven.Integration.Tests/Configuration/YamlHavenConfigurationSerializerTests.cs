using Haven.Application.Configuration;
using Haven.Infrastructure.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Integration.Tests.Configuration;

[TestFixture]
[Category("Integration")]
public sealed class YamlHavenConfigurationSerializerTests
{
    private string _testDirectory = null!;
    private string _originalDirectory = null!;
    private YamlHavenConfigurationSerializer _sut = null!;

    private string ConfigPath => Path.Combine(_testDirectory, "haven.yml");

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"haven-config-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        var logger = Substitute.For<ILogger<YamlHavenConfigurationSerializer>>();
        var manifestsOptions = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        manifestsOptions.CurrentValue.Returns(new ManifestsOptions { ManifestsPath = _testDirectory });
        _sut = new YamlHavenConfigurationSerializer(logger, manifestsOptions);
    }

    [TearDown]
    public void Cleanup()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Test]
    public async Task ReadAsync_ShouldReturnDefaults_AndCreateFile_WhenConfigFileDoesNotExist()
    {
        var result = await _sut.ReadAsync(CancellationToken.None);

        result.ShouldNotBeNull();
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
        var nestedDir = Path.Combine(_testDirectory, "nested");
        Directory.Delete(_testDirectory, recursive: true);
        var manifestsOptions = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        manifestsOptions.CurrentValue.Returns(new ManifestsOptions { ManifestsPath = nestedDir });
        var logger = Substitute.For<ILogger<YamlHavenConfigurationSerializer>>();
        _sut = new YamlHavenConfigurationSerializer(logger, manifestsOptions);
        Directory.CreateDirectory(_testDirectory);

        Directory.Exists(nestedDir).ShouldBeFalse();

        await _sut.WriteAsync(new HavenConfiguration(), CancellationToken.None);

        Directory.Exists(nestedDir).ShouldBeTrue();
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
