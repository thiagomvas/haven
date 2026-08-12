using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Dashboard.Queries.GetDashboardOverview;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Dashboard.Queries.GetDashboardOverview;

[Category("Unit")]
public sealed class GetDashboardOverviewHandlerTests
{
    private IProjectRepository _projectRepository;
    private GetDashboardOverviewHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _sut = new GetDashboardOverviewHandler(_projectRepository);
    }

    [Test]
    public async Task Handle_ShouldReturnZeroedOverview_WhenThereAreNoProjects()
    {
        _projectRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable());

        var result = await _sut.Handle(new GetDashboardOverviewQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalProjects.ShouldBe(0);
        result.Value.TotalEnvironments.ShouldBe(0);
        result.Value.ServiceStatistics.Total.ShouldBe(0);
        result.Value.AttentionEnvironment.ShouldBeNull();
        result.Value.DeploymentsLast24h.ShouldBe(0);
        result.Value.LastDeployment.ShouldBeNull();
    }

    [Test]
    public async Task Handle_ShouldPickMostSevereEnvironment_WhenMultipleProjectsHaveIssues()
    {
        var healthyProject = Project.Create("healthy-project");
        var healthyEnv = healthyProject.AddEnvironment("prod");
        var healthyService = healthyEnv.AddService("api", ServiceType.DockerImage, ExposureMode.Internal);
        healthyEnv.DeployService(healthyService.Id);

        var stoppedProject = Project.Create("stopped-project");
        var stoppedEnv = stoppedProject.AddEnvironment("prod");
        stoppedEnv.AddService("worker", ServiceType.DockerImage, ExposureMode.Internal);

        var degradedProject = Project.Create("degraded-project");
        var degradedEnv = degradedProject.AddEnvironment("prod");
        var runningService = degradedEnv.AddService("web", ServiceType.DockerImage, ExposureMode.Internal);
        degradedEnv.AddService("queue", ServiceType.DockerImage, ExposureMode.Internal);
        degradedEnv.DeployService(runningService.Id);

        _projectRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(healthyProject, stoppedProject, degradedProject));

        var result = await _sut.Handle(new GetDashboardOverviewQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalProjects.ShouldBe(3);
        result.Value.TotalEnvironments.ShouldBe(3);
        result.Value.ServiceStatistics.Total.ShouldBe(4);
        result.Value.ServiceStatistics.Running.ShouldBe(2);

        result.Value.AttentionEnvironment.ShouldNotBeNull();
        result.Value.AttentionEnvironment.ProjectName.ShouldBe("degraded-project");
        result.Value.AttentionEnvironment.Status.ShouldBe(HealthStatus.Degraded);
        result.Value.AttentionEnvironment.AffectedServiceCount.ShouldBe(1);
    }

    [Test]
    public async Task Handle_ShouldCountRunningButUnhealthyServices_AsAffected()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("prod");
        var unhealthyService = environment.AddService("api", ServiceType.DockerImage, ExposureMode.Internal);
        environment.DeployService(unhealthyService.Id);
        unhealthyService.Health = ServiceHealth.Unhealthy;

        var healthyService = environment.AddService("worker", ServiceType.DockerImage, ExposureMode.Internal);
        environment.DeployService(healthyService.Id);

        _projectRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(project));

        var result = await _sut.Handle(new GetDashboardOverviewQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AttentionEnvironment.ShouldNotBeNull();
        result.Value.AttentionEnvironment.AffectedServiceCount.ShouldBe(1);
    }

    [Test]
    public async Task Handle_ShouldCountRecentDeploysAndReportTheLatestOne()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("prod");
        var recentService = environment.AddService("api", ServiceType.DockerImage, ExposureMode.Internal);
        recentService.LastDeployedAt = DateTime.UtcNow.AddHours(-1);

        var oldService = environment.AddService("worker", ServiceType.DockerImage, ExposureMode.Internal);
        oldService.LastDeployedAt = DateTime.UtcNow.AddDays(-3);

        _projectRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(project));

        var result = await _sut.Handle(new GetDashboardOverviewQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DeploymentsLast24h.ShouldBe(1);
        result.Value.LastDeployment.ShouldNotBeNull();
        result.Value.LastDeployment.ServiceName.ShouldBe("api");
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
