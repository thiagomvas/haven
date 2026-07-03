using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
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
using NSubstitute.ExceptionExtensions;

using Shouldly;

using Environment = Haven.Domain.Entities.Environment;
using ServiceStatus = Haven.Domain.ServiceStatus;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class DockerfileDeployServiceTests
{
    private DockerfileDeployService _sut = null!;
    private ILogger<DockerfileDeployService> _logger = null!;
    private IDockerClient _client = null!;
    private INetworkingServiceFactory _networkingServiceFactory = null!;
    private INetworkingService _networkingService = null!;
    private IEnvironmentVariableService _environmentVariableService = null!;
    private IFeatureFlagService _featureFlagService = null!;
    private IGitService _gitService = null!;
    private IDeploymentLogService _logService = null!;
    private HavenDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<DockerfileDeployService>>();
        _client = Substitute.For<IDockerClient>();
        _db = TestDbContextFactory.CreateUnitDbContext();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _networkingService = Substitute.For<INetworkingService>();
        _featureFlagService = Substitute.For<IFeatureFlagService>();
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _gitService = Substitute.For<IGitService>();

        _featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());

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
                }
            });

        _networkingServiceFactory.Create(Arg.Any<ServiceType>()).Returns(_networkingService);
        _networkingService.DisconnectServiceFromAllNetworksAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _logService = Substitute.For<IDeploymentLogService>();

        _sut = new DockerfileDeployService(
            _logger, _client, _networkingServiceFactory,
            _environmentVariableService, _featureFlagService,
            _gitService, _logService, _db);
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Dispose();
        _client.Dispose();
    }

    [Test]
    public void ServiceType_ShouldBeDockerfile()
    {
        _sut.ServiceType.ShouldBe(ServiceType.Dockerfile);
    }

    [Test]
    public async Task DeployAsync_WhenEnvironmentIsNull_ShouldReturnNotFound()
    {
        var service = new Service();

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task DeployAsync_WhenDockerfileConfigIsNull_ShouldReturnValidationError()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.DockerImage);

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }


    [Test]
    public async Task DeployAsync_WithRawSource_ShouldBuildDockerImage()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Images.Received(1).BuildImageFromDockerfileAsync(
            Arg.Any<ImageBuildParameters>(),
            Arg.Any<Stream>(),
            Arg.Any<IEnumerable<AuthConfig>>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<IProgress<JSONMessage>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WithRawSource_ShouldCreateAndStartContainer()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).StartContainerAsync(
            "test-container-id", Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WithRawSource_ShouldReturnSuccess()
    {
        var (service, project, environment) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task DeployAsync_WithGitSource_WhenRepositoryDoesNotExist_ShouldClone()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Git);
        var repoPath = Path.Combine(Path.GetTempPath(), $"haven-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(repoPath);
        File.WriteAllText(Path.Combine(repoPath, "Dockerfile"), "FROM ubuntu:22.04");

        _gitService.ServiceRepositoryExists(service.Id).Returns(false);
        _gitService.CloneServiceRepositoryAsync(service.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(repoPath));
        _gitService.GetServiceRepositoryPath(service.Id).Returns(repoPath);

        try
        {
            await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

            await _gitService.Received(1).CloneServiceRepositoryAsync(
                service.Id, Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(repoPath))
                Directory.Delete(repoPath, recursive: true);
        }
    }

    [Test]
    public async Task DeployAsync_WithGitSource_WhenRepositoryExists_ShouldPull()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Git);
        var repoPath = Path.Combine(Path.GetTempPath(), $"haven-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(repoPath);
        File.WriteAllText(Path.Combine(repoPath, "Dockerfile"), "FROM ubuntu:22.04");

        _gitService.ServiceRepositoryExists(service.Id).Returns(true);
        _gitService.PullServiceRepositoryAsync(service.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _gitService.GetServiceRepositoryPath(service.Id).Returns(repoPath);

        try
        {
            await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

            await _gitService.Received(1).PullServiceRepositoryAsync(
                service.Id, Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _gitService.DidNotReceive().CloneServiceRepositoryAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(repoPath))
                Directory.Delete(repoPath, recursive: true);
        }
    }

    [Test]
    public async Task DeployAsync_WithGitSource_WhenPullFails_ShouldProceedWithExistingCode()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Git);
        var repoPath = Path.Combine(Path.GetTempPath(), $"haven-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(repoPath);
        File.WriteAllText(Path.Combine(repoPath, "Dockerfile"), "FROM ubuntu:22.04");

        _gitService.ServiceRepositoryExists(service.Id).Returns(true);
        _gitService.PullServiceRepositoryAsync(service.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failed);
        _gitService.GetServiceRepositoryPath(service.Id).Returns(repoPath);

        try
        {
            var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            await _client.Images.Received(1).BuildImageFromDockerfileAsync(
                Arg.Any<ImageBuildParameters>(), Arg.Any<Stream>(), Arg.Any<IEnumerable<AuthConfig>>(),
                Arg.Any<IDictionary<string, string>>(), Arg.Any<IProgress<JSONMessage>>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            if (Directory.Exists(repoPath))
                Directory.Delete(repoPath, recursive: true);
        }
    }

    [Test]
    public async Task DeployAsync_WithGitSource_WhenCloneFails_ShouldReturnFailure()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Git);

        _gitService.ServiceRepositoryExists(service.Id).Returns(false);
        _gitService.CloneServiceRepositoryAsync(service.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failed);

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task DeployAsync_ShouldUseImageTagWithServiceId()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);
        var expectedTag = $"haven-service-{service.Id:N}";

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Images.Received(1).BuildImageFromDockerfileAsync(
            Arg.Is<ImageBuildParameters>(p => p.Tags.Contains(expectedTag)),
            Arg.Any<Stream>(), Arg.Any<IEnumerable<AuthConfig>>(), Arg.Any<IDictionary<string, string>>(),
            Arg.Any<IProgress<JSONMessage>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WithInternalExposure_ShouldSetListenAddressTo127()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw, ExposureMode.Internal);

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.Env != null && p.Env.Contains("LISTEN_ADDRESS=127.0.0.1")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WithExternalExposure_ShouldSetListenAddressTo0000()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw, ExposureMode.External);

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.Env != null && p.Env.Contains("LISTEN_ADDRESS=0.0.0.0")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WithNoneExposure_ShouldNotSetListenAddress()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw, ExposureMode.None);

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.Env == null || !p.Env.Any(e => e.StartsWith("LISTEN_ADDRESS="))),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WithExistingContainer_ShouldRemoveItFirst()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);
        var existingContainerId = "existing-container-id";

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>
            {
                new() { ID = existingContainerId, State = "exited", Names = ["/old-container"] }
            });

        await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        await _client.Containers.Received(1).RemoveContainerAsync(
            existingContainerId, Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WhenContainerStartFails_ShouldReturnFailure()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);

        _client.Containers
            .StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.DeployAsync(service, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task StopAsync_WhenNoContainersFound_ShouldReturnNotFound()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());

        var result = await _sut.StopAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task StopAsync_WhenContainerExists_ShouldRemoveIt()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);
        var containerId = "running-container-id";

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>
            {
                new() { ID = containerId, State = "running", Names = ["/my-container"] }
            });

        var result = await _sut.StopAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _client.Containers.Received(1).StopContainerAsync(containerId, Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).RemoveContainerAsync(containerId, Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_WhenDockerfileConfigIsNull_ShouldReturnValidationError()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.DockerImage);

        var result = await _sut.StartAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task StartAsync_WithRawSource_ShouldSucceed()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);

        var result = await _sut.StartAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task StartAsync_WithRawSource_ShouldCreateAndStartContainer()
    {
        var (service, _, _) = SetupValidServiceWithProject(ServiceType.Dockerfile, DockerfileSource.Raw);

        await _sut.StartAsync(service, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>());
    }

    private (Service service, Project project, Environment environment) SetupValidServiceWithProject(
        ServiceType serviceType = ServiceType.Dockerfile,
        DockerfileSource? dockerfileSource = null,
        ExposureMode exposureMode = ExposureMode.Internal)
    {
        var project = Project.Create("TestProject", description: "A test project");
        var environment = project.AddEnvironment("dev", description: "Development");

        ServiceSourceConfig? config = serviceType switch
        {
            ServiceType.Dockerfile when dockerfileSource == DockerfileSource.Git => new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "https://github.com/example/repo.git",
                Branch = "main"
            },
            ServiceType.Dockerfile when dockerfileSource == DockerfileSource.Raw => new DockerfileConfig
            {
                Source = DockerfileSource.Raw,
                Content = "FROM ubuntu:22.04\nRUN echo hello"
            },
            ServiceType.DockerImage => new DockerConfig { Image = "nginx:latest" },
            _ => null
        };

        var service = project.AddService(environment.Id, "test-service", serviceType, exposureMode, sourceConfig: config);

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