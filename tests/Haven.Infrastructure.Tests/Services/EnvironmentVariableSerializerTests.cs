using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class EnvironmentVariableSerializerTests
{
    private EnvironmentVariableSerializer _sut = null!;
    private IProjectRepository _projectRepository = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IEnvironmentVariableRepository _envVarRepository = null!;
    private ILogger<EnvironmentVariableSerializer> _logger = null!;
    private IOptionsMonitor<ManifestsOptions> _optionsMonitor = null!;
    private string _testDirectory = null!;
    private string _originalDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _envVarRepository = Substitute.For<IEnvironmentVariableRepository>();
        _logger = Substitute.For<ILogger<EnvironmentVariableSerializer>>();
        _optionsMonitor = Substitute.For<IOptionsMonitor<ManifestsOptions>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), $"haven-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDirectory);

        _sut = new EnvironmentVariableSerializer(
            _projectRepository,
            _environmentRepository,
            _serviceRepository,
            _envVarRepository,
            _optionsMonitor,
            _logger);

        SetupDefaultMocks();
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    private void SetupDefaultMocks()
    {
        _optionsMonitor.CurrentValue.Returns(new ManifestsOptions());
        _envVarRepository.GetForProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _envVarRepository.GetForEnvironmentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _envVarRepository.GetForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    #region WriteExampleForProjectAsync Tests

    [Test]
    public async Task WriteExampleForProjectAsync_WithValidProject_ShouldWriteFile()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        var variables = CreateEnvironmentVariables(3, project.Id, EnvironmentVariableParentType.Project);
        _envVarRepository.GetForProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(variables);

        var result = await _sut.WriteExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var filePath = PathResolver.ProjectEnvExamplePath(project);
        File.Exists(filePath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(filePath);
        content.ShouldContain("KEY1=value1");
    }

    [Test]
    public async Task WriteExampleForProjectAsync_WithProjectNotFound_ShouldReturnFailure()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.WriteExampleForProjectAsync(projectId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForProjectAsync_WithNoVariables_ShouldReturnSuccess()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _envVarRepository.GetForProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.WriteExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForProjectAsync_ShouldCreateDirectoryIfNotExists()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        var variables = CreateEnvironmentVariables(1, project.Id, EnvironmentVariableParentType.Project);
        _envVarRepository.GetForProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(variables);

        await _sut.WriteExampleForProjectAsync(project.Id, CancellationToken.None);

        var filePath = PathResolver.ProjectEnvExamplePath(project);
        var directory = Path.GetDirectoryName(filePath);
        directory.ShouldNotBeNull();
        Directory.Exists(directory).ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForProjectAsync_WithComplexValues_ShouldFormatCorrectly()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        var variables = new[]
        {
            new EnvironmentVariables { Key = "DATABASE_URL", Value = "postgresql://localhost/db", ParentId = project.Id },
            new EnvironmentVariables { Key = "API_KEY", Value = "secret with spaces", ParentId = project.Id }
        };
        _envVarRepository.GetForProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(variables);

        var result = await _sut.WriteExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var filePath = PathResolver.ProjectEnvExamplePath(project);
        var content = await File.ReadAllTextAsync(filePath);
        content.ShouldContain("DATABASE_URL=");
        content.ShouldContain("API_KEY=");
    }

    #endregion

    #region WriteExampleForEnvironmentAsync Tests

    [Test]
    public async Task WriteExampleForEnvironmentAsync_WithValidEnvironment_ShouldWriteFile()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development environment");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        var variables = CreateEnvironmentVariables(2, environment.Id, EnvironmentVariableParentType.Environment);
        _envVarRepository.GetForEnvironmentAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(variables);

        var result = await _sut.WriteExampleForEnvironmentAsync(environment.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var filePath = PathResolver.EnvironmentEnvExamplePath(project, environment);
        File.Exists(filePath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForEnvironmentAsync_WithEnvironmentNotFound_ShouldReturnFailure()
    {
        var environmentId = Guid.NewGuid();
        _environmentRepository.GetByIdAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var result = await _sut.WriteExampleForEnvironmentAsync(environmentId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForEnvironmentAsync_WithProjectNotFound_ShouldReturnFailure()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.WriteExampleForEnvironmentAsync(environment.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForEnvironmentAsync_WithNoVariables_ShouldReturnSuccess()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.WriteExampleForEnvironmentAsync(environment.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region WriteExampleForServiceAsync Tests

    [Test]
    public async Task WriteExampleForServiceAsync_WithValidService_ShouldWriteFile()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        var variables = CreateEnvironmentVariables(1, service.Id, EnvironmentVariableParentType.Service);
        _envVarRepository.GetForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(variables);

        var result = await _sut.WriteExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var filePath = PathResolver.ServiceEnvExamplePath(project, environment, service);
        File.Exists(filePath).ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForServiceAsync_WithServiceNotFound_ShouldReturnFailure()
    {
        var serviceId = Guid.NewGuid();
        _serviceRepository.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((Service?)null);

        var result = await _sut.WriteExampleForServiceAsync(serviceId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForServiceAsync_WithEnvironmentNotFound_ShouldReturnFailure()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var result = await _sut.WriteExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task WriteExampleForServiceAsync_WithProjectNotFound_ShouldReturnFailure()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.WriteExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    #endregion

    #region ReadAndSyncExampleForProjectAsync Tests

    [Test]
    public async Task ReadAndSyncExampleForProjectAsync_WithValidFile_ShouldSyncVariables()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.ProjectEnvExamplePath(project);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "KEY1=value1\nKEY2=value2");

        var result = await _sut.ReadAndSyncExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.Received(1).CleanForProjectAsync(project.Id, Arg.Any<CancellationToken>());
        await _envVarRepository.Received(1).AddAsync(Arg.Is<List<EnvironmentVariables>>(l => l.Count == 2), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadAndSyncExampleForProjectAsync_WithProjectNotFound_ShouldReturnFailure()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.ReadAndSyncExampleForProjectAsync(projectId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForProjectAsync_WithMissingFile_ShouldReturnSuccess()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.ReadAndSyncExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.DidNotReceive().CleanForProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadAndSyncExampleForProjectAsync_WithEmptyFile_ShouldReturnSuccess()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.ProjectEnvExamplePath(project);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, string.Empty);

        var result = await _sut.ReadAndSyncExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.DidNotReceive().CleanForProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadAndSyncExampleForProjectAsync_WithCommentsAndBlankLines_ShouldSkipThem()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.ProjectEnvExamplePath(project);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var content = "# Comment\nKEY1=value1\n\n# Another comment\nKEY2=value2";
        await File.WriteAllTextAsync(filePath, content);

        var result = await _sut.ReadAndSyncExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.Received(1).AddAsync(Arg.Is<List<EnvironmentVariables>>(l => l.Count == 2), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadAndSyncExampleForProjectAsync_ShouldSetCorrectParentType()
    {
        var project = Project.Create("TestProject", description: "A test project");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.ProjectEnvExamplePath(project);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "KEY=value");

        var result = await _sut.ReadAndSyncExampleForProjectAsync(project.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.Received(1).AddAsync(
            Arg.Is<List<EnvironmentVariables>>(l =>
                l.All(v => v.ParentType == EnvironmentVariableParentType.Project && v.ParentId == project.Id)),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ReadAndSyncExampleForEnvironmentAsync Tests

    [Test]
    public async Task ReadAndSyncExampleForEnvironmentAsync_WithValidFile_ShouldSyncVariables()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.EnvironmentEnvExamplePath(project, environment);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "KEY=value");

        var result = await _sut.ReadAndSyncExampleForEnvironmentAsync(environment.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.Received(1).CleanForEnvironmentAsync(environment.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadAndSyncExampleForEnvironmentAsync_WithEnvironmentNotFound_ShouldReturnFailure()
    {
        var environmentId = Guid.NewGuid();
        _environmentRepository.GetByIdAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var result = await _sut.ReadAndSyncExampleForEnvironmentAsync(environmentId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForEnvironmentAsync_WithProjectNotFound_ShouldReturnFailure()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.ReadAndSyncExampleForEnvironmentAsync(environment.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForEnvironmentAsync_WithMissingFile_ShouldReturnSuccess()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.ReadAndSyncExampleForEnvironmentAsync(environment.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForEnvironmentAsync_ShouldSetCorrectParentType()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.EnvironmentEnvExamplePath(project, environment);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "KEY=value");

        var result = await _sut.ReadAndSyncExampleForEnvironmentAsync(environment.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.Received(1).AddAsync(
            Arg.Is<List<EnvironmentVariables>>(l =>
                l.All(v => v.ParentType == EnvironmentVariableParentType.Environment && v.ParentId == environment.Id)),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ReadAndSyncExampleForServiceAsync Tests

    [Test]
    public async Task ReadAndSyncExampleForServiceAsync_WithValidFile_ShouldSyncVariables()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.ServiceEnvExamplePath(project, environment, service);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "KEY=value");

        var result = await _sut.ReadAndSyncExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.Received(1).CleanForServiceAsync(service.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadAndSyncExampleForServiceAsync_WithServiceNotFound_ShouldReturnFailure()
    {
        var serviceId = Guid.NewGuid();
        _serviceRepository.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((Service?)null);

        var result = await _sut.ReadAndSyncExampleForServiceAsync(serviceId, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForServiceAsync_WithEnvironmentNotFound_ShouldReturnFailure()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var result = await _sut.ReadAndSyncExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForServiceAsync_WithProjectNotFound_ShouldReturnFailure()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.ReadAndSyncExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForServiceAsync_WithMissingFile_ShouldReturnSuccess()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.ReadAndSyncExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task ReadAndSyncExampleForServiceAsync_ShouldSetCorrectParentType()
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev");
        var service = environment.AddService("test-service", ServiceType.DockerImage, ExposureMode.Internal, null, new DockerConfig() { Image = "test" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var filePath = PathResolver.ServiceEnvExamplePath(project, environment, service);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "KEY=value");

        var result = await _sut.ReadAndSyncExampleForServiceAsync(service.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _envVarRepository.Received(1).AddAsync(
            Arg.Is<List<EnvironmentVariables>>(l =>
                l.All(v => v.ParentType == EnvironmentVariableParentType.Service && v.ParentId == service.Id)),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private static List<EnvironmentVariables> CreateEnvironmentVariables(
        int count,
        Guid parentId,
        EnvironmentVariableParentType parentType)
    {
        var variables = new List<EnvironmentVariables>();
        for (int i = 1; i <= count; i++)
        {
            variables.Add(new EnvironmentVariables
            {
                Key = $"KEY{i}",
                Value = $"value{i}",
                ParentId = parentId,
                ParentType = parentType
            });
        }
        return variables;
    }

    #endregion
}