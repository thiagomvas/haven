using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.BulkDeployServices;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Application.Features.Services.Shared;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Services;

[TestFixture]
[Category("Integration")]
public class BulkDeployServicesIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private IProjectRepository _projectRepository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _projectRepository = _fixture.GetService<IProjectRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
    }

    private async Task<(Guid ProjectId, Guid EnvironmentId)> CreateProjectAndEnvironmentAsync()
    {
        await _fixture.Client.PostAsJsonAsync("/api/projects", new { name = "Test Project" });
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", new { name = "staging" });
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        return (projectId, environmentId);
    }

    private async Task<Guid> CreateServiceAsync(Guid projectId, Guid environmentId, string name)
    {
        var serviceRequest = new CreateServiceCommand
        {
            Name = name,
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "nginx:latest" }
        };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        return apiResponse!.Data;
    }

    [Test]
    public async Task BulkDeployServices_WithAllValidServices_ReturnsSuccessForEach()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironmentAsync();
        var service1Id = await CreateServiceAsync(projectId, environmentId, "web");
        var service2Id = await CreateServiceAsync(projectId, environmentId, "api");

        var request = new BulkDeployServicesCommand { ServiceIds = [service1Id, service2Id] };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/bulk-deploy",
            request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BulkServiceActionResponse>>();
        apiResponse!.Success.ShouldBeTrue();
        apiResponse.Data!.SucceededCount.ShouldBe(2);
        apiResponse.Data.FailedCount.ShouldBe(0);
    }

    [Test]
    public async Task BulkDeployServices_WithMixOfValidAndInvalidServices_ReturnsPartialFailure()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironmentAsync();
        var serviceId = await CreateServiceAsync(projectId, environmentId, "web");
        var missingServiceId = Guid.NewGuid();

        var request = new BulkDeployServicesCommand { ServiceIds = [serviceId, missingServiceId] };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/bulk-deploy",
            request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BulkServiceActionResponse>>();
        apiResponse!.Data!.SucceededCount.ShouldBe(1);
        apiResponse.Data.FailedCount.ShouldBe(1);
        apiResponse.Data.Results.Single(r => r.ServiceId == missingServiceId).Success.ShouldBeFalse();
    }

    [Test]
    public async Task BulkDeployServices_WithEmptyServiceIds_ReturnsValidationError()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironmentAsync();

        var request = new BulkDeployServicesCommand { ServiceIds = [] };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/bulk-deploy",
            request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task BulkDeployServices_WithInvalidEnvironmentId_ReturnsNotFound()
    {
        var (projectId, _) = await CreateProjectAndEnvironmentAsync();
        var invalidEnvironmentId = Guid.NewGuid();

        var request = new BulkDeployServicesCommand { ServiceIds = [Guid.NewGuid()] };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{invalidEnvironmentId}/services/bulk-deploy",
            request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}