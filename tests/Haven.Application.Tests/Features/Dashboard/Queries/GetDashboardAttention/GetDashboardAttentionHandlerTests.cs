using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Dashboard.Queries.GetDashboardAttention;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Dashboard.Queries.GetDashboardAttention;

[Category("Unit")]
public sealed class GetDashboardAttentionHandlerTests
{
    private IProjectRepository _projectRepository;
    private IServiceStateReader _stateReader;
    private Dictionary<Guid, ServiceRegistryEntry> _entries;
    private HashSet<Guid> _failedDeploymentIds;
    private GetDashboardAttentionHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _stateReader = Substitute.For<IServiceStateReader>();
        _entries = [];
        _failedDeploymentIds = [];
        _stateReader.LoadAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new ServiceStateSnapshot(
                _entries,
                new Dictionary<Guid, DeploymentStatus>(),
                _failedDeploymentIds)));
        _sut = new GetDashboardAttentionHandler(_projectRepository, _stateReader);
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyList_WhenEverythingIsHealthy()
    {
        var project = Project.Create("shop");
        var service = project.AddEnvironment("prod").AddService("api", ServiceType.DockerImage, ExposureMode.Internal);
        SetState(service.Id, ServiceRuntimeState.Running);
        _projectRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(project));

        var result = await _sut.Handle(new GetDashboardAttentionQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldReportEachReasonWithItsContext()
    {
        var project = Project.Create("shop");
        var env = project.AddEnvironment("prod");
        var unhealthy = env.AddService("worker", ServiceType.DockerImage, ExposureMode.Internal);
        var crashed = env.AddService("cache", ServiceType.DockerImage, ExposureMode.Internal);
        var failedDeploy = env.AddService("api", ServiceType.DockerImage, ExposureMode.Internal);
        failedDeploy.LastDeployedAt = DateTime.UtcNow.AddMinutes(-5);
        SetState(unhealthy.Id, ServiceRuntimeState.Running, ServiceHealth.Unhealthy);
        SetState(crashed.Id, ServiceRuntimeState.Crashed);
        SetState(failedDeploy.Id, ServiceRuntimeState.Running);
        _failedDeploymentIds.Add(failedDeploy.Id);
        _projectRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(project));

        var result = await _sut.Handle(new GetDashboardAttentionQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(3);
        result.Value.Single(x => x.ServiceName == "worker").Reason.ShouldBe(AttentionReason.Unhealthy);
        result.Value.Single(x => x.ServiceName == "worker").Since.ShouldBeNull();
        result.Value.Single(x => x.ServiceName == "cache").Reason.ShouldBe(AttentionReason.Crashed);
        result.Value.Single(x => x.ServiceName == "cache").Since.ShouldNotBeNull();
        result.Value.Single(x => x.ServiceName == "api").Reason.ShouldBe(AttentionReason.DeploymentFailed);
        result.Value.All(x => x.ProjectName == "shop" && x.EnvironmentName == "prod").ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ShouldIgnoreIntentionallyStoppedServices()
    {
        var project = Project.Create("shop");
        var service = project.AddEnvironment("prod").AddService("migrator", ServiceType.DockerImage, ExposureMode.Internal);
        SetState(service.Id, ServiceRuntimeState.Stopped);
        _projectRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(project));

        var result = await _sut.Handle(new GetDashboardAttentionQuery(), CancellationToken.None);

        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldListHardFailuresBeforeUnhealthyServices_AndHonourTheLimit()
    {
        var project = Project.Create("shop");
        var env = project.AddEnvironment("prod");
        var unhealthy = env.AddService("aaa-unhealthy", ServiceType.DockerImage, ExposureMode.Internal);
        var crashed = env.AddService("zzz-crashed", ServiceType.DockerImage, ExposureMode.Internal);
        SetState(unhealthy.Id, ServiceRuntimeState.Running, ServiceHealth.Unhealthy);
        SetState(crashed.Id, ServiceRuntimeState.Crashed);
        _projectRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(project));

        var result = await _sut.Handle(new GetDashboardAttentionQuery { Limit = 1 }, CancellationToken.None);

        result.Value.Count.ShouldBe(1);
        result.Value[0].ServiceName.ShouldBe("zzz-crashed");
    }

    private void SetState(Guid serviceId, ServiceRuntimeState runtimeState, ServiceHealth health = ServiceHealth.Healthy)
    {
        var entry = ServiceRegistryEntry.Create(serviceId);
        entry.ApplyRuntimeState(runtimeState, DateTime.UtcNow);
        entry.ApplyHealth(health);
        _entries[serviceId] = entry;
    }

    private static async IAsyncEnumerable<Project> ToAsyncEnumerable(params Project[] projects)
    {
        foreach (var project in projects)
        {
            yield return project;
        }

        await Task.CompletedTask;
    }
}