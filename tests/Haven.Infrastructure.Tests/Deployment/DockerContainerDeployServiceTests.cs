using Docker.DotNet;
using Docker.DotNet.Models;
using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Persistence;
using Haven.Testing.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;
using ServiceStatus = Haven.Domain.ServiceStatus;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class DockerContainerDeployServiceTests
{
    private DockerContainerDeployService _sut = null!;
    private ILogger<DockerContainerDeployService> _logger = null!;
    private IDockerClient _client;
    private INetworkingServiceFactory _networkingServiceFactory;
    private HavenDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<DockerContainerDeployService>>();
        _client = Substitute.For<IDockerClient>();
        _db = TestDbContextFactory.CreateUnitDbContext();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();

        // Default mocks
        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());

        _client.Images
            .CreateImageAsync(Arg.Any<ImagesCreateParameters>(), Arg.Any<AuthConfig>(), Arg.Any<IProgress<JSONMessage>>(),Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _client.Containers
            .CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "test-container-id" });

        _client.Containers
            .StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(true);
        
        _networkingServiceFactory.Create(Arg.Any<ServiceType>())
            .Returns(Substitute.For<INetworkingService>());

        _sut = new DockerContainerDeployService(_logger, _db, _client, _networkingServiceFactory);
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Dispose();
        _client.Dispose();
    }

    [Test]
    public void ServiceType_ShouldReturnDockerImage()
    {
        _sut.ServiceType.ShouldBe(ServiceType.DockerImage);
    }

    [Test]
    public async Task DeployAsync_WhenEnvironmentIsNull_ShouldReturnNotFoundError()
    {
        var service = new Service();
        service.GetType().GetProperty(nameof(Service.Environment))?.SetValue(service, null);

        var result = await _sut.DeployAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Environment");
    }

    [Test]
    public async Task DeployAsync_WhenSuccessful_ShouldLogInformation()
    {
        var (service, project, _) = SetupValidServiceWithProject();

        await _sut.DeployAsync(service, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Deploying service") && x.ToString()!.Contains("Docker Container")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task DeployAsync_WhenSuccessful_ShouldCallProjectDeployService()
    {
        var (service, project, environment) = SetupValidServiceWithProject();

        await _sut.DeployAsync(service, CancellationToken.None);

        var updatedProject = _db.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .First(p => p.Id == project.Id);

        var updatedService = updatedProject.Environments
            .First(e => e.Id == environment.Id)
            .Services
            .First(s => s.Id == service.Id);

        updatedService.Status.ShouldBe(ServiceStatus.Running);
    }

    [Test]
    public async Task DeployAsync_WhenSuccessful_ShouldSaveChangesToDatabase()
    {
        var (service, project, environment) = SetupValidServiceWithProject();

        var result = await _sut.DeployAsync(service, CancellationToken.None);

        var savedProject = _db.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .First(p => p.Id == project.Id);

        var savedService = savedProject.Environments
            .First(e => e.Id == environment.Id)
            .Services
            .First(s => s.Id == service.Id);

        savedService.ShouldNotBeNull();
        savedService.Status.ShouldBe(ServiceStatus.Running);
    }

    [Test]
    public async Task DeployAsync_WhenSuccessful_ShouldReturnSuccessResult()
    {
        var (service, _, _) = SetupValidServiceWithProject();

        var result = await _sut.DeployAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task DeployAsync_ShouldLogWithServiceAndProjectNames()
    {
        var (service, project, _) = SetupValidServiceWithProject();

        await _sut.DeployAsync(service, CancellationToken.None);

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains(service.Name) && x.ToString()!.Contains(project.Name)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task DeployAsync_WhenExistingContainerExists_ShouldRemoveItFirst()
    {
        var (service, project, _) = SetupValidServiceWithProject();
        var existingContainerId = "existing-container-id";
        var containersList = new List<ContainerListResponse>
        {
            new() { ID = existingContainerId, State = "exited", Names = new List<string> { "/old-container" } }
        };

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(containersList);

        await _sut.DeployAsync(service, CancellationToken.None);

        await _client.Containers.Received(1).RemoveContainerAsync(existingContainerId, Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WhenExistingRunningContainerExists_ShouldStopItBeforeRemoving()
    {
        var (service, project, _) = SetupValidServiceWithProject();
        var existingContainerId = "existing-running-container-id";
        var containersList = new List<ContainerListResponse>
        {
            new() { ID = existingContainerId, State = "running", Names = new List<string> { "/old-container" } }
        };

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(containersList);

        await _sut.DeployAsync(service, CancellationToken.None);

        await _client.Containers.Received(1).StopContainerAsync(existingContainerId, Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).RemoveContainerAsync(existingContainerId, Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    private (Service service, Project project, Environment environment) SetupValidServiceWithProject()
    {
        var project = Project.Create("TestProject", "A test project");
        var environment = project.AddEnvironment("dev", "Development");
        var dockerConfig = new DockerConfig { Image = "nginx:latest" };
        var service = project.AddService(environment.Id, "api-service", ServiceType.DockerImage, ExposureMode.Internal, dockerConfig);

        _db.Projects.Add(project);
        _db.SaveChanges();

        _db.ChangeTracker.Clear();
        var trackedProject = _db.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .First(p => p.Id == project.Id);

        var trackedEnvironment = trackedProject.Environments.First(e => e.Id == environment.Id);
        var trackedService = trackedEnvironment.Services.First(s => s.Id == service.Id);

        return (trackedService, trackedProject, trackedEnvironment);
    }

}
