using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.HealthChecks;
using Haven.Application.Features.HealthChecks.Commands.CreateHealthCheckCommand;
using Haven.Application.Features.HealthChecks.Commands.TestHealthCheckCommand;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.HealthChecks;

[TestFixture]
[Category("Integration")]
public class HealthCheckIntegrationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private IntegrationTestFixture _fixture = null!;
    private string _healthChecksUrl = null!;
    private Guid _serviceId;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();

        await _fixture.Client.PostAsJsonAsync("/api/projects", new { name = "Test Project" });
        var projects = await _fixture.GetService<IProjectRepository>().GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", new { name = "dev" });
        var project = await _fixture.GetService<IProjectRepository>().GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            new CreateServiceCommand
            {
                Name = "web-api",
                Type = ServiceType.DockerImage,
                ExposureMode = ExposureMode.None,
                DockerConfig = new DockerConfig { Image = "nginx:latest" }
            });
        _serviceId = (await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;

        _healthChecksUrl = $"/api/projects/{projectId}/environments/{environmentId}/services/{_serviceId}/health-checks";
    }

    [TearDown]
    public void TearDown() => _fixture?.Dispose();

    private async Task<Guid> CreateContainerCheckAsync(int retries = 0, int failureThreshold = 1)
    {
        var response = await _fixture.Client.PostAsJsonAsync(_healthChecksUrl, new CreateHealthCheckCommand
        {
            Name = "container",
            Kind = HealthCheckKind.Container,
            Retries = retries,
            FailureThreshold = failureThreshold
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;
    }

    [Test]
    public async Task RunNow_RunsSynchronouslyAndReturnsWhyTheCheckCouldNotPass()
    {
        var id = await CreateContainerCheckAsync();

        var response = await _fixture.Client.PostAsJsonAsync($"{_healthChecksUrl}/{id}/run", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<HealthCheckResultDto>>(Json);
        body!.Data!.Status.ShouldNotBe(ServiceHealth.Healthy); // the service has never been deployed, so there is no container
        body.Data.Message.ShouldNotBeNullOrWhiteSpace();
        body.Data.RanAt.ShouldNotBe(default);
    }

    [Test]
    public async Task RunNow_PersistsResultThatIsListedInHistoryAndOnTheCheck()
    {
        var id = await CreateContainerCheckAsync();
        await _fixture.Client.PostAsJsonAsync($"{_healthChecksUrl}/{id}/run", new { });
        await _fixture.Client.PostAsJsonAsync($"{_healthChecksUrl}/{id}/run", new { });

        var history = await _fixture.Client.GetFromJsonAsync<ApiResponse<List<HealthCheckResultDto>>>(
            $"{_healthChecksUrl}/{id}/results?limit=10", Json);
        var checks = await _fixture.Client.GetFromJsonAsync<ApiResponse<List<HealthCheckDto>>>(
            _healthChecksUrl, Json);

        history!.Data!.Count.ShouldBe(2);
        history.Data[0].RanAt.ShouldBeGreaterThanOrEqualTo(history.Data[1].RanAt);
        var check = checks!.Data!.ShouldHaveSingleItem();
        check.LastRunAt.ShouldNotBeNull();
        check.LastRunMessage.ShouldBe(history.Data[0].Message);
        check.LastRunReason.ShouldBe(history.Data[0].Reason);
    }

    [Test]
    public async Task RunNow_UnknownHealthCheck_ReturnsNotFound()
    {
        var response = await _fixture.Client.PostAsJsonAsync($"{_healthChecksUrl}/{Guid.NewGuid()}/run", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Create_StoresRetriesAndThresholds()
    {
        await CreateContainerCheckAsync(retries: 2, failureThreshold: 3);

        var checks = await _fixture.Client.GetFromJsonAsync<ApiResponse<List<HealthCheckDto>>>(
            _healthChecksUrl, Json);

        var check = checks!.Data!.ShouldHaveSingleItem();
        check.Retries.ShouldBe(2);
        check.FailureThreshold.ShouldBe(3);
        check.SuccessThreshold.ShouldBe(1);
    }

    [Test]
    public async Task Create_WithOutOfRangeRetries_IsRejected()
    {
        var response = await _fixture.Client.PostAsJsonAsync(_healthChecksUrl, new CreateHealthCheckCommand
        {
            Name = "bad",
            Kind = HealthCheckKind.Container,
            Retries = 99
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task Test_RunsUnsavedConfigurationWithoutPersistingAnything()
    {
        var response = await _fixture.Client.PostAsJsonAsync($"{_healthChecksUrl}/test",
            new TestHealthCheckCommand { Kind = HealthCheckKind.Container });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<HealthCheckResultDto>>(Json);
        body!.Data!.Message.ShouldNotBeNullOrWhiteSpace();

        var checks = await _fixture.Client.GetFromJsonAsync<ApiResponse<List<HealthCheckDto>>>(
            _healthChecksUrl, Json);
        checks!.Data.ShouldBeEmpty();
    }

    [Test]
    public async Task Test_WithInvalidConfigForKind_IsRejected()
    {
        var response = await _fixture.Client.PostAsJsonAsync($"{_healthChecksUrl}/test",
            new TestHealthCheckCommand { Kind = HealthCheckKind.Tcp, Config = """{"host":"db","port":0}""" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
