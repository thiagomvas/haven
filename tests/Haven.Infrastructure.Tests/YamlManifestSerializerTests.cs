using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects;
using Haven.Application.Features.Services;
using Haven.Application.Mappers;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Infrastructure.Tests;

[Category("Unit")]
public sealed class YamlManifestSerializerTests
{
    private YamlManifestSerializer _sut = null!;
    private ILogger<YamlManifestSerializer> _logger = null!;
    private string _testDirectory = null!;
    private string _manifestsDirectory = null!;
    private string _originalDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<YamlManifestSerializer>>();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"haven-tests-{Guid.NewGuid()}");
        _manifestsDirectory = Path.Combine(_testDirectory, "manifests");

        Directory.CreateDirectory(_manifestsDirectory);

        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        _sut = new YamlManifestSerializer(_logger);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Test]
    public async Task WriteProjectAsync_ShouldCreateProjectDirectory()
    {
        var project = CreateProject();

        await _sut.WriteProjectAsync(project, CancellationToken.None);

        var projectPath = Path.Combine(_manifestsDirectory, "projects", project.Name);
        Directory.Exists(projectPath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteProjectAsync_ShouldCreateProjectYamlFile()
    {
        var project = CreateProject();

        await _sut.WriteProjectAsync(project, CancellationToken.None);

        var filePath = Path.Combine(_manifestsDirectory, "projects", project.Name, "project.yaml");
        File.Exists(filePath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteProjectAsync_ShouldSerializeProjectCorrectly()
    {
        var project = CreateProject("TestProject", "A test project");

        await _sut.WriteProjectAsync(project, CancellationToken.None);

        var filePath = Path.Combine(_manifestsDirectory, "projects", project.Name, "project.yaml");
        var yaml = await File.ReadAllTextAsync(filePath);

        yaml.ShouldContain("name: TestProject");
        yaml.ShouldContain("A test project");
        yaml.ShouldContain(project.Id.ToString());
    }

    [Test]
    public async Task WriteProjectAsync_ShouldLogSuccess()
    {
        var project = CreateProject();

        await _sut.WriteProjectAsync(project, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Project manifest written")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task DeleteProjectAsync_ShouldRemoveProjectDirectory()
    {
        var project = CreateProject();

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.DeleteProjectAsync(project, CancellationToken.None);

        var projectPath = Path.Combine(_manifestsDirectory, "projects", project.Name);
        Directory.Exists(projectPath).ShouldBeFalse();
    }

    [Test]
    public async Task DeleteProjectAsync_ShouldLogSuccess()
    {
        var project = CreateProject();

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.DeleteProjectAsync(project, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Project manifest deleted")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task DeleteProjectAsync_ShouldNotThrowWhenDirectoryDoesNotExist()
    {
        var project = CreateProject();

        await _sut.DeleteProjectAsync(project, CancellationToken.None);
    }

    [Test]
    public async Task ReadProjectsAsync_ShouldReturnEmptyListWhenNoManifestsDirectory()
    {
        Directory.Delete(_manifestsDirectory, recursive: true);

        var result = await _sut.ReadProjectsAsync(CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task ReadProjectsAsync_ShouldReturnEmptyListWhenNoProjects()
    {
        var result = await _sut.ReadProjectsAsync(CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task ReadProjectsAsync_ShouldReadSingleProject()
    {
        var project = CreateProject("MyProject");

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        var result = await _sut.ReadProjectsAsync(CancellationToken.None);

        result.ShouldHaveSingleItem();
        result[0].Name.ShouldBe("MyProject");
        result[0].Id.ShouldBe(project.Id);
    }

    [Test]
    public async Task ReadProjectsAsync_ShouldReadMultipleProjects()
    {
        var project1 = CreateProject("Project1");
        var project2 = CreateProject("Project2");

        await _sut.WriteProjectAsync(project1, CancellationToken.None);
        await _sut.WriteProjectAsync(project2, CancellationToken.None);

        var result = await _sut.ReadProjectsAsync(CancellationToken.None);

        result.Count.ShouldBe(2);
        result.Select(p => p.Name).ShouldContain("Project1");
        result.Select(p => p.Name).ShouldContain("Project2");
    }

    [Test]
    public async Task WriteEnvironmentAsync_ShouldCreateEnvironmentDirectory()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);

        var envPath = Path.Combine(_manifestsDirectory, "projects", project.Name, "environments", environment.Name);
        Directory.Exists(envPath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteEnvironmentAsync_ShouldCreateEnvironmentYamlFile()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);

        var filePath = Path.Combine(_manifestsDirectory, "projects", project.Name, "environments", environment.Name, "environment.yaml");
        File.Exists(filePath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteEnvironmentAsync_ShouldSerializeEnvironmentCorrectly()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project, "staging", "Staging Environment");

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);

        var filePath = Path.Combine(_manifestsDirectory, "projects", project.Name, "environments", environment.Name, "environment.yaml");
        var yaml = await File.ReadAllTextAsync(filePath);

        yaml.ShouldContain("name: staging");
        yaml.ShouldContain("Staging Environment");
        yaml.ShouldContain(environment.Id.ToString());
    }

    [Test]
    public async Task DeleteEnvironmentAsync_ShouldRemoveEnvironmentDirectory()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);
        await _sut.DeleteEnvironmentAsync(project, environment.Name, CancellationToken.None);

        var envPath = Path.Combine(_manifestsDirectory, "projects", project.Name, "environments", environment.Name);
        Directory.Exists(envPath).ShouldBeFalse();
    }

    [Test]
    public async Task WriteServiceAsync_ShouldCreateServiceDirectory()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);
        var service = CreateService(project, environment);

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);
        await _sut.WriteServiceAsync(project, environment, service, CancellationToken.None);

        var servicePath = Path.Combine(
            _manifestsDirectory, "projects", project.Name,
            "environments", environment.Name, "services", service.Name);
        Directory.Exists(servicePath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteServiceAsync_ShouldCreateServiceYamlFile()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);
        var service = CreateService(project, environment);

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);
        await _sut.WriteServiceAsync(project, environment, service, CancellationToken.None);

        var filePath = Path.Combine(
            _manifestsDirectory, "projects", project.Name,
            "environments", environment.Name, "services", service.Name, "service.yaml");
        File.Exists(filePath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteServiceAsync_ShouldSerializeServiceCorrectly()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);
        var service = CreateService(project, environment, "api-service");

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);
        await _sut.WriteServiceAsync(project, environment, service, CancellationToken.None);

        var filePath = Path.Combine(
            _manifestsDirectory, "projects", project.Name,
            "environments", environment.Name, "services", service.Name, "service.yaml");
        var yaml = await File.ReadAllTextAsync(filePath);

        yaml.ShouldContain("name: api-service");
        yaml.ShouldContain(service.Id.ToString());
    }

    [Test]
    public async Task DeleteServiceAsync_ShouldRemoveServiceDirectory()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);
        var service = CreateService(project, environment);

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);
        await _sut.WriteServiceAsync(project, environment, service, CancellationToken.None);
        await _sut.DeleteServiceAsync(project, environment, service.Name, CancellationToken.None);

        var servicePath = Path.Combine(
            _manifestsDirectory, "projects", project.Name,
            "environments", environment.Name, "services", service.Name);
        Directory.Exists(servicePath).ShouldBeFalse();
    }

    [Test]
    public async Task ReadProjectsAsync_ShouldIncludeEnvironmentsAndServices()
    {
        var project = CreateProject();
        var environment = CreateEnvironment(project);
        var service = CreateService(project, environment);

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);
        await _sut.WriteServiceAsync(project, environment, service, CancellationToken.None);

        var results = await _sut.ReadProjectsAsync(CancellationToken.None);

        results.ShouldHaveSingleItem();
        var readProject = results[0];
        readProject.Environments.ShouldHaveSingleItem();

        var readEnv = readProject.Environments[0];
        readEnv.Services.ShouldHaveSingleItem();
    }

    [Test]
    public async Task RoundTrip_ProjectWithEnvironmentsAndServices()
    {
        var project = CreateProject("TestProject", "Test Description");
        var environment = CreateEnvironment(project, "dev", "Development");
        var service = CreateService(project, environment, "web-api");

        await _sut.WriteProjectAsync(project, CancellationToken.None);
        await _sut.WriteEnvironmentAsync(project, environment, CancellationToken.None);
        await _sut.WriteServiceAsync(project, environment, service, CancellationToken.None);

        var results = await _sut.ReadProjectsAsync(CancellationToken.None);

        results.ShouldHaveSingleItem();
        var readProject = results[0];
        readProject.Name.ShouldBe("TestProject");
        readProject.Description.ShouldBe("Test Description");
        readProject.Id.ShouldBe(project.Id);

        readProject.Environments.ShouldHaveSingleItem();
        var readEnv = readProject.Environments[0];
        readEnv.Name.ShouldBe("dev");

        readEnv.Services.ShouldHaveSingleItem();
        var readService = readEnv.Services[0];
        readService.Name.ShouldBe("web-api");
    }

    [Test]
    public async Task ReadProjectsAsync_ShouldSkipDirectoriesWithoutProjectYaml()
    {
        var projectPath = Path.Combine(_manifestsDirectory, "projects", "InvalidProject");
        Directory.CreateDirectory(projectPath);

        var result = await _sut.ReadProjectsAsync(CancellationToken.None);

        result.ShouldBeEmpty();
    }

    private static Project CreateProject(string name = "TestProject", string? description = "A test project")
    {
        return Project.Create(name, description);
    }

    private static Haven.Domain.Entities.Environment CreateEnvironment(Project project, string name = "test", string? description = "Test Environment")
    {
        return project.AddEnvironment(name, description);
    }

    private static Haven.Domain.Entities.Service CreateService(Project project, Haven.Domain.Entities.Environment environment, string name = "test-service")
    {
        return project.AddService(environment.Id, name, ServiceType.DockerImage, ExposureMode.Internal);
    }
}
