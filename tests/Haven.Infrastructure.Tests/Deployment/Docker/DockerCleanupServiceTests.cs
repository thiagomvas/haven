using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;
using Haven.Testing.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Docker;

[Category("Unit")]
public sealed class DockerCleanupServiceTests
{
    private IDockerClient _client = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private IServiceScope _scope = null!;
    private IServiceProvider _serviceProvider = null!;
    private HavenDbContext _db = null!;
    private ILogger<DockerCleanupService> _logger = null!;
    private DockerCleanupService _sut = null!;

    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

    [SetUp]
    public void Setup()
    {
        _client = Substitute.For<IDockerClient>();
        _db = TestDbContextFactory.CreateUnitDbContext();
        _logger = Substitute.For<ILogger<DockerCleanupService>>();

        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scope = Substitute.For<IServiceScope>();
        _serviceProvider = Substitute.For<IServiceProvider>();

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(HavenDbContext)).Returns(_db);

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());
        _client.Images
            .ListImagesAsync(Arg.Any<ImagesListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ImagesListResponse>());

        _sut = new DockerCleanupService(_client, _scopeFactory, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Dispose();
        _client.Dispose();
        _scope.Dispose();
    }

    private (Service service, string imageTag) SeedService()
    {
        var project = Project.Create("TestProject", alias: "testproject");
        var environment = project.AddEnvironment("dev", alias: "dev");
        var service = project.AddService(environment.Id, "test-service", ServiceType.Dockerfile, ExposureMode.Internal, alias: "svc");

        _db.Projects.Add(project);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        var imageTag = DockerUtils.BuildImageTag("testproject", "dev", "svc", service.Id);
        return (service, imageTag);
    }

    private void SetupContainers(params ContainerListResponse[] containers)
        => _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(containers.ToList());

    private void SetupImages(params ImagesListResponse[] images)
        => _client.Images
            .ListImagesAsync(Arg.Any<ImagesListParameters>(), Arg.Any<CancellationToken>())
            .Returns(images.ToList());

    // --- Containers ---

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ContainerWithNoServiceLabel_IsIgnored()
    {
        SeedService();
        SetupContainers(new ContainerListResponse
        {
            ID = "c1",
            Labels = new Dictionary<string, string>(),
            Created = DateTime.UtcNow.AddDays(-2)
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedContainerIds.ShouldBeEmpty();
        await _client.Containers.DidNotReceive().RemoveContainerAsync(
            Arg.Any<string>(), Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ContainerBelongsToExistingService_IsNotRemoved()
    {
        var (service, _) = SeedService();
        SetupContainers(new ContainerListResponse
        {
            ID = "c1",
            Labels = new Dictionary<string, string> { { "haven.service.id", service.Id.ToString() } },
            Created = DateTime.UtcNow.AddDays(-2),
            State = "running"
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedContainerIds.ShouldBeEmpty();
        await _client.Containers.DidNotReceive().RemoveContainerAsync(
            Arg.Any<string>(), Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ContainerHasNoMatchingServiceAndPastGracePeriod_IsRemoved()
    {
        SeedService();
        SetupContainers(new ContainerListResponse
        {
            ID = "orphan-1",
            Labels = new Dictionary<string, string> { { "haven.service.id", Guid.NewGuid().ToString() } },
            Created = DateTime.UtcNow.AddDays(-2),
            State = "exited"
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedContainerIds.ShouldContain("orphan-1");
        await _client.Containers.Received(1).RemoveContainerAsync(
            "orphan-1", Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_RunningOrphanedContainer_IsStoppedBeforeRemoval()
    {
        SeedService();
        SetupContainers(new ContainerListResponse
        {
            ID = "orphan-1",
            Labels = new Dictionary<string, string> { { "haven.service.id", Guid.NewGuid().ToString() } },
            Created = DateTime.UtcNow.AddDays(-2),
            State = "running"
        });

        await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        await _client.Containers.Received(1).StopContainerAsync(
            "orphan-1", Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ContainerWithinGracePeriod_IsNotRemoved()
    {
        SeedService();
        SetupContainers(new ContainerListResponse
        {
            ID = "orphan-1",
            Labels = new Dictionary<string, string> { { "haven.service.id", Guid.NewGuid().ToString() } },
            Created = DateTime.UtcNow.AddHours(-1),
            State = "exited"
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedContainerIds.ShouldBeEmpty();
        await _client.Containers.DidNotReceive().RemoveContainerAsync(
            Arg.Any<string>(), Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_DryRun_ReportsOrphanedContainerButDoesNotRemoveIt()
    {
        SeedService();
        SetupContainers(new ContainerListResponse
        {
            ID = "orphan-1",
            Labels = new Dictionary<string, string> { { "haven.service.id", Guid.NewGuid().ToString() } },
            Created = DateTime.UtcNow.AddDays(-2),
            State = "running"
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: true, CancellationToken.None);

        result.RemovedContainerIds.ShouldContain("orphan-1");
        await _client.Containers.DidNotReceive().RemoveContainerAsync(
            Arg.Any<string>(), Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.DidNotReceive().StopContainerAsync(
            Arg.Any<string>(), Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>());
    }

    // --- Images ---

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ImageTagMatchesExistingService_IsNotRemoved()
    {
        var (_, imageTag) = SeedService();
        SetupImages(new ImagesListResponse
        {
            ID = "img1",
            RepoTags = [$"{imageTag}:latest"],
            Created = DateTime.UtcNow.AddDays(-2)
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedImageTags.ShouldBeEmpty();
        await _client.Images.DidNotReceive().DeleteImageAsync(
            Arg.Any<string>(), Arg.Any<ImageDeleteParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ImageWithNoMatchingServiceAndPastGracePeriod_IsRemoved()
    {
        SeedService();
        SetupImages(new ImagesListResponse
        {
            ID = "img1",
            RepoTags = ["haven-orphaned-project-dev-old:latest"],
            Created = DateTime.UtcNow.AddDays(-2)
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedImageTags.ShouldContain("haven-orphaned-project-dev-old:latest");
        await _client.Images.Received(1).DeleteImageAsync(
            "haven-orphaned-project-dev-old:latest", Arg.Any<ImageDeleteParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ImageWithoutHavenPrefix_IsIgnored()
    {
        SeedService();
        SetupImages(new ImagesListResponse
        {
            ID = "img1",
            RepoTags = ["nginx:latest"],
            Created = DateTime.UtcNow.AddDays(-2)
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedImageTags.ShouldBeEmpty();
        await _client.Images.DidNotReceive().DeleteImageAsync(
            Arg.Any<string>(), Arg.Any<ImageDeleteParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_ImageWithinGracePeriod_IsNotRemoved()
    {
        SeedService();
        SetupImages(new ImagesListResponse
        {
            ID = "img1",
            RepoTags = ["haven-orphaned-project-dev-old:latest"],
            Created = DateTime.UtcNow.AddHours(-1)
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: false, CancellationToken.None);

        result.RemovedImageTags.ShouldBeEmpty();
        await _client.Images.DidNotReceive().DeleteImageAsync(
            Arg.Any<string>(), Arg.Any<ImageDeleteParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CleanupOrphanedResourcesAsync_DryRun_ReportsOrphanedImageButDoesNotDeleteIt()
    {
        SeedService();
        SetupImages(new ImagesListResponse
        {
            ID = "img1",
            RepoTags = ["haven-orphaned-project-dev-old:latest"],
            Created = DateTime.UtcNow.AddDays(-2)
        });

        var result = await _sut.CleanupOrphanedResourcesAsync(GracePeriod, dryRun: true, CancellationToken.None);

        result.RemovedImageTags.ShouldContain("haven-orphaned-project-dev-old:latest");
        await _client.Images.DidNotReceive().DeleteImageAsync(
            Arg.Any<string>(), Arg.Any<ImageDeleteParameters>(), Arg.Any<CancellationToken>());
    }
}