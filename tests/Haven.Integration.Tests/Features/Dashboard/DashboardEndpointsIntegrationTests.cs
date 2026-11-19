using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Dashboard.Queries.GetDashboardAttention;
using Haven.Application.Features.Deployments.Queries.GetRecentDeployments;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Dashboard;

[TestFixture]
[Category("Integration")]
public class DashboardEndpointsIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private IntegrationTestFixture _fixture = null!;
    private HavenDbContext _dbContext = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _dbContext = _fixture.GetService<HavenDbContext>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetRecentDeployments_ReturnsNewestFirstWithProjectAndEnvironmentNames()
    {
        var project = Project.Create("shop");
        var environment = project.AddEnvironment("prod");
        var service = environment.AddService("api", ServiceType.DockerImage, ExposureMode.Internal);
        _dbContext.Projects.Add(project);

        var older = Deployment.Create(service.Id, "old.log", "webhook");
        older.StartedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var newer = Deployment.Create(service.Id, "new.log", "thiago");
        _dbContext.Deployments.AddRange(older, newer);
        await _dbContext.SaveChangesAsync();

        var response = await _fixture.Client.GetAsync("/api/dashboard/deployments?limit=5");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<RecentDeploymentDto>>>(JsonOptions);
        body.ShouldNotBeNull();
        body.Data.Count.ShouldBe(2);
        body.Data[0].Id.ShouldBe(newer.Id);
        body.Data[0].ServiceName.ShouldBe("api");
        body.Data[0].EnvironmentName.ShouldBe("prod");
        body.Data[0].ProjectName.ShouldBe("shop");
    }

    [Test]
    public async Task GetAttention_ReturnsEmptyList_WhenNothingNeedsAttention()
    {
        var response = await _fixture.Client.GetAsync("/api/dashboard/attention");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AttentionServiceDto>>>(JsonOptions);
        body.ShouldNotBeNull();
        body.Data.ShouldBeEmpty();
    }
}