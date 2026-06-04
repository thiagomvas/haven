using Haven.Application.Configuration;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Persistence.Manifests;
using Haven.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Haven.Integration.Tests.Persistence.Manifests;

[TestFixture]
[Category("Integration")]
public class ProjectManifestSerializerTests
{
    private ProjectManifestSerializer _sut = null!;
    private string _testDirectory = null!;
    private string _originalDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        var logger = Substitute.For<ILogger<ProjectManifestSerializer>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), $"haven-manifest-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        var optionsMonitor = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        optionsMonitor.CurrentValue.Returns(new ManifestsOptions { ManifestsPath = _testDirectory });
        PathResolver.Initialize(optionsMonitor);

        _sut = new ProjectManifestSerializer(logger);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Test]
    public async Task WriteAsync_WithValidProject_WritesYamlFile()
    {
        // Arrange
        var project = Project.Create("My Project", description: "A project description");

        // Act
        await _sut.WriteAsync(project, CancellationToken.None);

        // Assert
        var filePath = PathResolver.ProjectFilePath(project);
        File.Exists(filePath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.ShouldContain(project.Name);
        content.ShouldContain("A project description");
    }

    [Test]
    public async Task WriteAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var project = Project.Create("My Project", description: "A project description");
        var projectDir = PathResolver.ProjectPath(project);
        Directory.Exists(projectDir).ShouldBeFalse();

        // Act
        await _sut.WriteAsync(project, CancellationToken.None);

        // Assert
        Directory.Exists(projectDir).ShouldBeTrue();
    }

    [Test]
    public async Task ReadAsync_WithValidProjects_ReadsAllProjectManifests()
    {
        // Arrange
        var project1 = Project.Create("Project One", description: "First project");
        var project2 = Project.Create("Project Two", description: "Second project");
        var project3 = Project.Create("Project Three", description: "Third project");

        // Write manifests
        await _sut.WriteAsync(project1, CancellationToken.None);
        await _sut.WriteAsync(project2, CancellationToken.None);
        await _sut.WriteAsync(project3, CancellationToken.None);

        // Act
        var projects = await _sut.ReadAsync(ct: CancellationToken.None);

        // Assert
        projects.Count.ShouldBe(3);
        projects.Select(p => p.Name).ShouldContain("Project One");
        projects.Select(p => p.Name).ShouldContain("Project Two");
        projects.Select(p => p.Name).ShouldContain("Project Three");
    }

    [Test]
    public async Task ReadAsync_WithNoManifests_ReturnsEmpty()
    {
        // Act
        var projects = await _sut.ReadAsync(ct: CancellationToken.None);

        // Assert
        projects.ShouldBeEmpty();
    }

    [Test]
    public async Task WriteAndReadAsync_PreservesProjectData()
    {
        // Arrange
        var originalProject = Project.Create("Test Project", description: "Test description");

        // Act - Write
        await _sut.WriteAsync(originalProject, CancellationToken.None);

        // Act - Read
        var readProjects = await _sut.ReadAsync(ct: CancellationToken.None);
        var readProject = readProjects.First();

        // Assert
        readProject.Name.ShouldBe(originalProject.Name);
        readProject.Description.ShouldBe(originalProject.Description);
        readProject.Id.ShouldBe(originalProject.Id);
    }

    [Test]
    public async Task RenameAsync_RenamesProjectDirectory()
    {
        // Arrange
        var project = Project.Create("OldName", description: "A project");
        await _sut.WriteAsync(project, CancellationToken.None);

        var oldPath = PathResolver.ProjectPath("OldName");
        var newPath = PathResolver.ProjectPath("NewName");
        Directory.Exists(oldPath).ShouldBeTrue();

        // Act
        await _sut.RenameAsync(project, "OldName", "NewName", CancellationToken.None);

        // Assert
        Directory.Exists(oldPath).ShouldBeFalse();
        Directory.Exists(newPath).ShouldBeTrue();
    }

    [Test]
    public async Task RemoveAsync_DeletesProjectDirectory()
    {
        // Arrange
        var project = Project.Create("My Project", description: "A project");
        await _sut.WriteAsync(project, CancellationToken.None);

        var path = PathResolver.ProjectPath(project);
        Directory.Exists(path).ShouldBeTrue();

        // Act
        await _sut.RemoveAsync(project, CancellationToken.None);

        // Assert
        Directory.Exists(path).ShouldBeFalse();
    }

    [Test]
    public async Task WriteAsync_WithMultipleProjects_CreatesCorrectStructure()
    {
        // Arrange
        var project = Project.Create("Test Project", description: "A test project");

        // Add environments and services
        var devEnv = project.AddEnvironment("dev", description: "Development");
        var stagingEnv = project.AddEnvironment("staging", description: "Staging");

        // Act
        await _sut.WriteAsync(project, CancellationToken.None);

        // Assert
        var projectDir = PathResolver.ProjectPath(project);
        Directory.Exists(projectDir).ShouldBeTrue();

        var filePath = PathResolver.ProjectFilePath(project);
        File.Exists(filePath).ShouldBeTrue();

        var content = await File.ReadAllTextAsync(filePath);
        content.ShouldContain("Test Project");
    }

    [Test]
    public async Task ReadAsync_SkipsDirectoriesWithoutProjectFile()
    {
        // Arrange
        var goodProject = Project.Create("Good Project", description: "A valid project");
        await _sut.WriteAsync(goodProject, CancellationToken.None);

        // Create a directory without a project.yaml file
        var projectsDir = PathResolver.ProjectsDirectory;
        var emptyDir = Path.Combine(projectsDir, "Empty Project");
        Directory.CreateDirectory(emptyDir);

        // Act
        var projects = await _sut.ReadAsync(ct: CancellationToken.None);

        // Assert
        projects.Count.ShouldBe(1);
        projects.First().Name.ShouldBe("Good Project");
    }
}
