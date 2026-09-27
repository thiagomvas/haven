using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services.Queries;
using Haven.Application.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Services;

[TestFixture]
[Category("Integration")]
public class ServiceComputedOutputsIntegrationTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private IntegrationTestFixture _fixture = null!;
    private IProjectRepository _projectRepository = null!;
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _projectRepository = _fixture.GetService<IProjectRepository>();
        _serviceRegistryEntryRepository = _fixture.GetService<IServiceRegistryEntryRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
    }

    private async Task<(Guid ProjectId, Guid EnvironmentId, Guid ServiceId)> CreatePostgresServiceAsync()
    {
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", new { name = "Test Project" });
        projectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var envResponse = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", new { name = "staging" });
        envResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var templateRequest = new CreateServiceFromTemplateCommand
        {
            InputValues = new Dictionary<string, string> { { "postgres_password", "s3cret" } }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/from-template/postgres",
            templateRequest);
        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var response = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        return (projectId, environmentId, response!.Data);
    }

    [Test]
    public async Task Dashboard_ForNotYetRunningService_ReportsComputedOutputUnavailable()
    {
        var (projectId, environmentId, serviceId) = await CreatePostgresServiceAsync();

        var dashboardResponse = await _fixture.Client.GetAsync($"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}/dashboard");
        dashboardResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<ApiResponse<ServiceDashboardDto>>(ResponseJsonOptions);

        var output = dashboard!.Data!.ComputedOutputs.Single();
        output.IsAvailable.ShouldBeFalse();
        output.UnavailableReason.ShouldBe("NotRunning");
    }

    [Test]
    public async Task Dashboard_ForRunningService_ReturnsMaskedPreview_AndRevealEndpointReturnsPlaintext()
    {
        var (projectId, environmentId, serviceId) = await CreatePostgresServiceAsync();

        var registry = ServiceRegistryEntry.Create(serviceId);
        registry.UpdateRuntime("172.18.0.5", [], ServiceStatus.Running);
        await _serviceRegistryEntryRepository.InsertAsync(registry, CancellationToken.None);
        await _fixture.GetService<Haven.Infrastructure.Persistence.HavenDbContext>().SaveChangesAsync();

        var dashboardResponse = await _fixture.Client.GetAsync($"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}/dashboard");
        dashboardResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<ApiResponse<ServiceDashboardDto>>(ResponseJsonOptions);

        var output = dashboard!.Data!.ComputedOutputs.Single();
        output.IsAvailable.ShouldBeTrue();
        output.Preview.ShouldNotContain("s3cret");
        output.Preview.ShouldContain("172.18.0.5");

        var revealResponse = await _fixture.Client.GetAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}/outputs/{output.Key}/value");
        revealResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var revealed = await revealResponse.Content.ReadFromJsonAsync<ApiResponse<ComputedOutputValueDto>>(ResponseJsonOptions);

        revealed!.Data!.Value.ShouldBe("postgresql://postgres:s3cret@172.18.0.5:5432/postgres");
    }
}