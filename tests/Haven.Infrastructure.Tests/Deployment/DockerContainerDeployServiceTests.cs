using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Persistence;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private IEnvironmentVariableService _environmentVariableService;
    private IFeatureFlagService _featureFlagService;
    private IDeploymentLogService _logService = null!;
    private HavenDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<DockerContainerDeployService>>();
        _client = Substitute.For<IDockerClient>();
        _db = TestDbContextFactory.CreateUnitDbContext();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Default mocks
        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());

        _client.Images
            .CreateImageAsync(Arg.Any<ImagesCreateParameters>(), Arg.Any<AuthConfig>(), Arg.Any<IProgress<JSONMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _client.Containers
            .CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "test-container-id" });

        _client.Containers
            .StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _client.Containers
            .InspectContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ContainerInspectResponse
            {
                NetworkSettings = new NetworkSettings
                {
                    Networks = new Dictionary<string, EndpointSettings>()
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>()
                }
            });

        _networkingServiceFactory.Create(Arg.Any<ServiceType>())
            .Returns(Substitute.For<INetworkingService>());

        _logService = Substitute.For<IDeploymentLogService>();

        var volumesOptions = Substitute.For<IOptionsMonitor<VolumesOptions>>();
        volumesOptions.CurrentValue.Returns(new VolumesOptions());

        _sut = new DockerContainerDeployService(_logger, _db, _client, _networkingServiceFactory, _environmentVariableService, _featureFlagService, _logService, volumesOptions);
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

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Environment");
    }

    [Test]
    public async Task DeployAsync_WhenSuccessful_ShouldLogInformation()
    {
        var (service, project, _) = SetupValidServiceWithProject();

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Deploying service") && x.ToString()!.Contains("Docker Container")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task DeployAsync_WhenSuccessful_ShouldReturnSuccessResult()
    {
        var (service, project, environment) = SetupValidServiceWithProject();

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }


    [Test]
    public async Task DeployAsync_ShouldLogWithServiceAndProjectNames()
    {
        var (service, project, _) = SetupValidServiceWithProject();

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains(service.Name) && x.ToString()!.Contains(project.Name)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task DeployAsync_WhenImagePullFails_ShouldReturnDockerInvalidImageError()
    {
        var (service, _, _) = SetupValidServiceWithProject();
        _client.Images
            .CreateImageAsync(Arg.Any<ImagesCreateParameters>(), Arg.Any<AuthConfig>(), Arg.Any<IProgress<JSONMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new DockerApiException(System.Net.HttpStatusCode.NotFound, "No such image")));

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Docker.InvalidImage);
    }

    [Test]
    public async Task DeployAsync_WhenImagePullFails_ShouldNotCreateContainer()
    {
        var (service, _, _) = SetupValidServiceWithProject();
        _client.Images
            .CreateImageAsync(Arg.Any<ImagesCreateParameters>(), Arg.Any<AuthConfig>(), Arg.Any<IProgress<JSONMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new DockerApiException(System.Net.HttpStatusCode.NotFound, "No such image")));

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Containers.DidNotReceive().CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>());
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

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

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

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Containers.Received(1).StopContainerAsync(existingContainerId, Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).RemoveContainerAsync(existingContainerId, Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_WhenEnvironmentIsNull_ShouldReturnNotFoundError()
    {
        var service = new Service();
        service.GetType().GetProperty(nameof(Service.Environment))?.SetValue(service, null);

        var result = await _sut.StartAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("Environment");
    }

    [Test]
    public async Task StartAsync_WhenSuccessful_ShouldReturnSuccessResult()
    {
        var (service, project, environment) = SetupValidServiceWithProject();

        var result = await _sut.StartAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task StartAsync_WhenSuccessful_ShouldCreateAndStartContainer()
    {
        var (service, project, _) = SetupValidServiceWithProject();

        await _sut.StartAsync(service, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_ShouldLogInformationAboutStarting()
    {
        var (service, project, _) = SetupValidServiceWithProject();

        await _sut.StartAsync(service, CancellationToken.None);

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains("Starting service")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task StartAsync_WhenExposureModeIsCustomAndPortHasExplicitIp_ShouldUseExplicitIpInPortBinding()
    {
        var (service, _, _) = SetupValidServiceWithProject(ExposureMode.Custom, ["192.168.1.5:8080:80"]);
        CreateContainerParameters? captured = null;
        _client.Containers
            .CreateContainerAsync(Arg.Do<CreateContainerParameters>(p => captured = p), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "test-container-id" });

        await _sut.StartAsync(service, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.HostConfig.PortBindings["80/tcp"][0].HostIP.ShouldBe("192.168.1.5");
        captured.HostConfig.PortBindings["80/tcp"][0].HostPort.ShouldBe("8080");
    }

    [Test]
    public async Task StartAsync_WhenExposureModeIsCustomAndPortOmitsIp_ShouldDefaultToAllInterfaces()
    {
        var (service, _, _) = SetupValidServiceWithProject(ExposureMode.Custom, ["8080:80"]);
        CreateContainerParameters? captured = null;
        _client.Containers
            .CreateContainerAsync(Arg.Do<CreateContainerParameters>(p => captured = p), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "test-container-id" });

        await _sut.StartAsync(service, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.HostConfig.PortBindings["80/tcp"][0].HostIP.ShouldBe("0.0.0.0");
    }

    [Test]
    public async Task StartAsync_WhenExposureModeIsCustom_ShouldSetListenAddressToAllInterfaces()
    {
        var (service, _, _) = SetupValidServiceWithProject(ExposureMode.Custom, ["192.168.1.5:8080:80"]);
        CreateContainerParameters? captured = null;
        _client.Containers
            .CreateContainerAsync(Arg.Do<CreateContainerParameters>(p => captured = p), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "test-container-id" });

        await _sut.StartAsync(service, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Env.ShouldContain("LISTEN_ADDRESS=0.0.0.0");
    }

    private (Service service, Project project, Environment environment) SetupValidServiceWithProject() =>
        SetupValidServiceWithProject(ExposureMode.Internal, []);

    private (Service service, Project project, Environment environment) SetupValidServiceWithProject(
        ExposureMode exposureMode, List<string> ports)
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");
        var dockerConfig = new DockerConfig { Image = "nginx:latest", Ports = ports };
        var service = project.AddService(environment.Id, "api-service", ServiceType.DockerImage, exposureMode, null, dockerConfig);

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