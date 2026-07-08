using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Persistence.Manifests;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Integration.Tests.Persistence.Manifests;

[TestFixture]
[Category("Integration")]
public class ServiceManifestSerializerTests
{
    private ServiceManifestSerializer _sut = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private string _testDirectory = null!;
    private string _originalDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        var logger = Substitute.For<ILogger<ServiceManifestSerializer>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), $"haven-manifest-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        var optionsMonitor = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        optionsMonitor.CurrentValue.Returns(new ManifestsOptions { ManifestsPath = _testDirectory });
        PathResolver.Initialize(optionsMonitor);

        var volumesOptions = Substitute.For<IOptionsMonitor<VolumesOptions>>();
        volumesOptions.CurrentValue.Returns(new VolumesOptions());

        _sut = new ServiceManifestSerializer(_environmentRepository, volumesOptions, logger);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Test]
    public async Task WriteAsync_WithValidService_WritesYamlFile()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        var dockerConfig = new DockerConfig { Image = "nginx:latest" };
        var service = environment.AddService("web-api", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);

        // Act
        await _sut.WriteAsync(service, CancellationToken.None);

        // Assert
        var filePath = PathResolver.ServiceFilePath(project.Name, environment.Name, service.Name);
        File.Exists(filePath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.ShouldContain(service.Name);
        content.ShouldContain("nginx:latest");
    }

    [Test]
    public async Task WriteAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        var dockerConfig = new DockerConfig { Image = "nginx:latest" };
        var service = environment.AddService("web-api", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);

        var serviceDir = PathResolver.ServicePath(project, environment, service);
        Directory.Exists(serviceDir).ShouldBeFalse();

        // Act
        await _sut.WriteAsync(service, CancellationToken.None);

        // Assert
        Directory.Exists(serviceDir).ShouldBeTrue();
    }

    [Test]
    public async Task ReadAsync_WithValidServices_ReadsAllServiceManifests()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);

        var dockerConfig = new DockerConfig { Image = "nginx:latest" };
        var api = environment.AddService("api", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);
        var db = environment.AddService("database", ServiceType.DockerImage, ExposureMode.Internal, null, dockerConfig);
        var cache = environment.AddService("cache", ServiceType.DockerImage, ExposureMode.Internal, null, dockerConfig);

        // Write manifests
        await _sut.WriteAsync(api, CancellationToken.None);
        await _sut.WriteAsync(db, CancellationToken.None);
        await _sut.WriteAsync(cache, CancellationToken.None);

        // Act
        var services = await _sut.ReadAsync(environment.Id, CancellationToken.None);

        // Assert
        services.Count.ShouldBe(3);
        services.Select(s => s.Name).ShouldContain("api");
        services.Select(s => s.Name).ShouldContain("database");
        services.Select(s => s.Name).ShouldContain("cache");
    }

    [Test]
    public async Task ReadAsync_WithEmptyParentId_ReturnsEmpty()
    {
        // Act
        var services = await _sut.ReadAsync(Guid.Empty, CancellationToken.None);

        // Assert
        services.ShouldBeEmpty();
    }

    [Test]
    public async Task ReadAsync_WithNonExistentEnvironment_ReturnsEmpty()
    {
        // Arrange
        _environmentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        // Act
        var services = await _sut.ReadAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        services.ShouldBeEmpty();
    }

    [Test]
    public async Task WriteAndReadAsync_PreservesServiceData()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("prod", description: "Production");
        var dockerConfig = new DockerConfig { Image = "nginx:1.21.0", Ports = ["8080"] };
        var originalService = environment.AddService("web", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);

        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);

        // Act - Write
        await _sut.WriteAsync(originalService, CancellationToken.None);

        // Act - Read
        var readServices = await _sut.ReadAsync(environment.Id, CancellationToken.None);
        var readService = readServices.First();

        // Assert
        readService.Name.ShouldBe(originalService.Name);
        readService.Type.ShouldBe(originalService.Type);
        readService.ExposureMode.ShouldBe(originalService.ExposureMode);
        readService.Id.ShouldBe(originalService.Id);
        readService.Token.ShouldBe(originalService.Token);
    }

    [Test]
    public async Task WriteAndReadAsync_PreservesToken()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("staging", description: "Staging");
        var dockerConfig = new DockerConfig { Image = "app:latest" };
        var originalService = environment.AddService("app", ServiceType.DockerImage, ExposureMode.Internal, null, dockerConfig);
        var originalToken = originalService.Token;

        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);

        // Act - Write
        await _sut.WriteAsync(originalService, CancellationToken.None);

        // Act - Read
        var readServices = await _sut.ReadAsync(environment.Id, CancellationToken.None);
        var readService = readServices.First();

        // Assert - Token should be preserved, not regenerated
        readService.Token.ShouldBe(originalToken);
    }

    [Test]
    public async Task RenameAsync_RenamesServiceDirectory()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        var dockerConfig = new DockerConfig { Image = "nginx:latest" };
        var service = environment.AddService("api", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);

        await _sut.WriteAsync(service, CancellationToken.None);

        var oldPath = PathResolver.ServicePath(project.Name, environment.Name, "api");
        var newPath = PathResolver.ServicePath(project.Name, environment.Name, "web-api");
        Directory.Exists(oldPath).ShouldBeTrue();

        // Act
        await _sut.RenameAsync(service, "api", "web-api", CancellationToken.None);

        // Assert
        Directory.Exists(oldPath).ShouldBeFalse();
        Directory.Exists(newPath).ShouldBeTrue();
    }

    [Test]
    public async Task RemoveAsync_DeletesServiceDirectory()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        var dockerConfig = new DockerConfig { Image = "nginx:latest" };
        var service = environment.AddService("api", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);

        await _sut.WriteAsync(service, CancellationToken.None);

        var path = PathResolver.ServicePath(project, environment, service);
        Directory.Exists(path).ShouldBeTrue();

        // Act
        await _sut.RemoveAsync(service, CancellationToken.None);

        // Assert
        Directory.Exists(path).ShouldBeFalse();
    }

    [Test]
    public async Task WriteAsync_WithComplexDockerConfig_PreservesAllProperties()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");

        var dockerConfig = new DockerConfig
        {
            Image = "myapp:1.2.3",
            Ports = ["8080", "8443"],
            RestartPolicy = RestartPolicy.Always
        };

        var service = environment.AddService("app", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);

        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);

        // Act
        await _sut.WriteAsync(service, CancellationToken.None);
        var readServices = await _sut.ReadAsync(environment.Id, CancellationToken.None);
        var readService = readServices.First();

        // Assert
        readService.SourceConfig.ShouldNotBeNull();
        readService.SourceConfig.ShouldBeOfType<DockerConfig>();

        var readConfig = (DockerConfig)readService.SourceConfig;
        readConfig.Image.ShouldBe("myapp:1.2.3");
        readConfig.Ports.ShouldBe(["8080", "8443"]);
        readConfig.RestartPolicy.ShouldBe(RestartPolicy.Always);
    }

    [Test]
    public async Task WriteAndReadAsync_PreservesVolumesRegardlessOfBackupEnabled()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        var dockerConfig = new DockerConfig { Image = "nginx:latest" };
        var service = environment.AddService("web", ServiceType.DockerImage, ExposureMode.External, null, dockerConfig);

        service.AddVolume(VolumeType.HostPath, "data", "/data", "/host/data", readOnly: false, backupEnabled: false);
        service.AddVolume(VolumeType.Named, "cache", "/cache", "cache-volume", readOnly: true, backupEnabled: true);

        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);

        // Act - Write
        await _sut.WriteAsync(service, CancellationToken.None);

        // Act - Read
        var readServices = await _sut.ReadAsync(environment.Id, CancellationToken.None);
        var readService = readServices.First();

        // Assert - both volumes round-trip, including the one with BackupEnabled = false
        readService.Volumes.Count.ShouldBe(2);

        var readData = readService.Volumes.Single(v => v.Name == "data");
        readData.Type.ShouldBe(VolumeType.HostPath);
        readData.Target.ShouldBe("/data");
        readData.Source.ShouldBe("/host/data");
        readData.ReadOnly.ShouldBeFalse();
        readData.BackupEnabled.ShouldBeFalse();

        var readCache = readService.Volumes.Single(v => v.Name == "cache");
        readCache.ReadOnly.ShouldBeTrue();
        readCache.BackupEnabled.ShouldBeTrue();
    }
}