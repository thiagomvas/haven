using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain.Aggregates;
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
public class EnvironmentManifestSerializerTests
{
    private EnvironmentManifestSerializer _sut = null!;
    private IProjectRepository _projectRepository = null!;
    private string _testDirectory = null!;
    private string _originalDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        var logger = Substitute.For<ILogger<EnvironmentManifestSerializer>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), $"haven-manifest-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        var optionsMonitor = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        optionsMonitor.CurrentValue.Returns(new ManifestsOptions { ManifestsPath = _testDirectory });
        PathResolver.Initialize(optionsMonitor);

        _sut = new EnvironmentManifestSerializer(_projectRepository, logger);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Test]
    public async Task WriteAsync_WithValidEnvironment_WritesYamlFile()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development environment");

        // Act
        await _sut.WriteAsync(environment, CancellationToken.None);

        // Assert
        var filePath = PathResolver.EnvironmentFilePath(project.Name, environment.Name);
        File.Exists(filePath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.ShouldContain(environment.Name);
        content.ShouldContain("Development environment");
    }

    [Test]
    public async Task WriteAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development environment");
        var environmentDir = PathResolver.EnvironmentPath(project, environment);
        Directory.Exists(environmentDir).ShouldBeFalse();

        // Act
        await _sut.WriteAsync(environment, CancellationToken.None);

        // Assert
        Directory.Exists(environmentDir).ShouldBeTrue();
    }

    [Test]
    public async Task ReadAsync_WithValidEnvironments_ReadsAllEnvironmentManifests()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var dev = project.AddEnvironment("dev", description: "Development");
        var staging = project.AddEnvironment("staging", description: "Staging");
        var prod = project.AddEnvironment("prod", description: "Production");

        // Write manifests
        await _sut.WriteAsync(dev, CancellationToken.None);
        await _sut.WriteAsync(staging, CancellationToken.None);
        await _sut.WriteAsync(prod, CancellationToken.None);

        // Act
        var environments = await _sut.ReadAsync(project.Id, CancellationToken.None);

        // Assert
        environments.Count.ShouldBe(3);
        environments.Select(e => e.Name).ShouldContain("dev");
        environments.Select(e => e.Name).ShouldContain("staging");
        environments.Select(e => e.Name).ShouldContain("prod");
    }

    [Test]
    public async Task ReadAsync_WithEmptyParentId_ReturnsEmpty()
    {
        // Act
        var environments = await _sut.ReadAsync(Guid.Empty, CancellationToken.None);

        // Assert
        environments.ShouldBeEmpty();
    }

    [Test]
    public async Task ReadAsync_WithNonExistentProject_ReturnsEmpty()
    {
        // Arrange
        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        // Act
        var environments = await _sut.ReadAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        environments.ShouldBeEmpty();
    }

    [Test]
    public async Task WriteAndReadAsync_PreservesEnvironmentData()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var originalEnvironment = project.AddEnvironment("staging", description: "Staging environment");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        // Act - Write
        await _sut.WriteAsync(originalEnvironment, CancellationToken.None);

        // Act - Read
        var readEnvironments = await _sut.ReadAsync(project.Id, CancellationToken.None);
        var readEnvironment = readEnvironments.First();

        // Assert
        readEnvironment.Name.ShouldBe(originalEnvironment.Name);
        readEnvironment.Description.ShouldBe(originalEnvironment.Description);
        readEnvironment.Id.ShouldBe(originalEnvironment.Id);
    }

    [Test]
    public async Task RenameAsync_RenamesEnvironmentDirectory()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        await _sut.WriteAsync(environment, CancellationToken.None);

        var oldPath = PathResolver.EnvironmentPath(project.Name, "dev");
        var newPath = PathResolver.EnvironmentPath(project.Name, "development");
        Directory.Exists(oldPath).ShouldBeTrue();

        // Act
        await _sut.RenameAsync(environment, "dev", "development", CancellationToken.None);

        // Assert
        Directory.Exists(oldPath).ShouldBeFalse();
        Directory.Exists(newPath).ShouldBeTrue();
    }

    [Test]
    public async Task RemoveAsync_DeletesEnvironmentDirectory()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        await _sut.WriteAsync(environment, CancellationToken.None);

        var path = PathResolver.EnvironmentPath(project, environment);
        Directory.Exists(path).ShouldBeTrue();

        // Act
        await _sut.RemoveAsync(environment, CancellationToken.None);

        // Assert
        Directory.Exists(path).ShouldBeFalse();
    }
}