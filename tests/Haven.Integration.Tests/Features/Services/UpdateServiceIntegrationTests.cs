using System.Net;
using System.Net.Http.Json;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Application.Features.Services.Commands.UpdateService;
using Haven.Domain;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;
using Shouldly;

namespace Haven.Integration.Tests.Features.Services;

[TestFixture]
[Category("Integration")]
public class UpdateServiceIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private IProjectRepository _projectRepository = null!;
    private IServiceRepository _serviceRepository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _projectRepository = _fixture.GetService<IProjectRepository>();
        _serviceRepository = _fixture.GetService<IServiceRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
    }

    [Test]
    public async Task UpdateService_WithValidInput_UpdatesServiceSuccessfully()
    {
        // Arrange - Create project, environment, and service
        var projectName = "Test Project";
        var projectRequest = new { name = projectName };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);

        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentName = "staging";
        var environmentRequest = new { name = environmentName, description = "Staging environment" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);

        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceName = "web-api";
        var serviceRequest = new CreateServiceCommand
        {
            Name = serviceName,
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "nginx:latest" }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        var apiResponse = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var serviceId = apiResponse!.Data;

        // Act - Update service name
        var updateRequest = new UpdateServiceCommand
        {
            Name = (Optional<string>)"web-app"
        };
        var updateResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}",
            updateRequest,
            _fixture.JsonSerializerOptions);

        // Assert - HTTP response
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updateApiResponse = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        updateApiResponse!.Success.ShouldBeTrue();
        updateApiResponse.Data.ShouldBe(serviceId);

        // Assert - Service was updated in database
        var service = await _serviceRepository.GetByIdAsync(serviceId, CancellationToken.None);
        service.ShouldNotBeNull();
        service.Name.ShouldBe("web-app");
    }

    [Test]
    public async Task UpdateService_WithExposureModeChange_UpdatesSuccessfully()
    {
        // Arrange
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "dev" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceRequest = new CreateServiceCommand
        {
            Name = "cache",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "redis:latest" }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        var apiResponse = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var serviceId = apiResponse!.Data;

        // Act - Change exposure mode from External to Internal
        var updateRequest = new UpdateServiceCommand
        {
            ExposureMode = (Optional<ExposureMode>)ExposureMode.Internal
        };
        var updateResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}",
            updateRequest,
            _fixture.JsonSerializerOptions);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var service = await _serviceRepository.GetByIdAsync(serviceId, CancellationToken.None);
        service!.ExposureMode.ShouldBe(ExposureMode.Internal);
    }

    [Test]
    public async Task UpdateService_WithMultipleFields_UpdatesAllSuccessfully()
    {
        // Arrange
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "prod" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceRequest = new CreateServiceCommand
        {
            Name = "api",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "myapp:v1" }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        var apiResponse = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var serviceId = apiResponse!.Data;

        // Act - Update multiple fields
        var updateRequest = new UpdateServiceCommand
        {
            Name = (Optional<string>)"api-service",
            ExposureMode = (Optional<ExposureMode>)ExposureMode.Internal,
            DockerConfig = (Optional<DockerConfig?>)new DockerConfig { Image = "myapp:v2" }
        };
        var updateResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}",
            updateRequest,
            _fixture.JsonSerializerOptions);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var service = await _serviceRepository.GetByIdAsync(serviceId, CancellationToken.None);
        service!.Name.ShouldBe("api-service");
        service.ExposureMode.ShouldBe(ExposureMode.Internal);
        service.SourceConfig.ShouldNotBeNull();
    }

    [Test]
    public async Task UpdateService_WithInvalidServiceId_ReturnsNotFound()
    {
        // Arrange
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "dev" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        // Act
        var invalidServiceId = Guid.NewGuid();
        var updateRequest = new UpdateServiceCommand { Name = (Optional<string>)"new-name" };
        var updateResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{invalidServiceId}",
            updateRequest,
            _fixture.JsonSerializerOptions);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateService_WithDuplicateName_ReturnsConflict()
    {
        // Arrange - Create two services
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var service1Request = new CreateServiceCommand
        {
            Name = "api",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "nginx" }
        };
        var service1Response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            service1Request);
        var apiResponse1 = await service1Response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var service1Id = apiResponse1!.Data;

        var service2Request = new CreateServiceCommand
        {
            Name = "web",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "nginx" }
        };
        var service2Response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            service2Request);
        var apiResponse2 = await service2Response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var service2Id = apiResponse2!.Data;

        // Act - Try to update service2 to have the same name as service1
        var updateRequest = new UpdateServiceCommand
        {
            Name = (Optional<string>)"api"
        };
        var updateResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{service2Id}",
            updateRequest,
            _fixture.JsonSerializerOptions);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task UpdateService_RaisesServiceUpdatedEvent()
    {
        // Arrange
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "dev" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceRequest = new CreateServiceCommand
        {
            Name = "web",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "nginx" }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        var apiResponse = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var serviceId = apiResponse!.Data;

        _fixture.EventCollector.Clear();

        // Act - Update the service
        var updateRequest = new UpdateServiceCommand
        {
            Name = "web-app"
        };
        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}",
            updateRequest,
            _fixture.JsonSerializerOptions);

        
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        // Verify service was updated
        var updatedService = await _serviceRepository.GetByIdAsync(serviceId, CancellationToken.None);
        updatedService!.Name.ShouldBe("web-app");

        // Assert - ServiceUpdatedEvent was raised
        _fixture.EventCollector.GetEventCount<ServiceUpdatedEvent>().ShouldBe(1);
    }

    [Test]
    public async Task UpdateService_WithSameName_DoesNotUpdate()
    {
        // Arrange
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceRequest = new CreateServiceCommand
        {
            Name = "web",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "nginx" }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        var apiResponse = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var serviceId = apiResponse!.Data;

        var originalService = await _serviceRepository.GetByIdAsync(serviceId, CancellationToken.None);
        var originalUpdatedAt = originalService!.UpdatedAt;

        await Task.Delay(100);

        _fixture.EventCollector.Clear();

        // Act - Update with the same name
        var updateRequest = new UpdateServiceCommand
        {
            Name = (Optional<string>)"web"
        };
        await _fixture.Client.PatchAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}",
            updateRequest,
            _fixture.JsonSerializerOptions);

        // Assert - No event should be raised and UpdatedAt should not change
        _fixture.EventCollector.GetEventCount<ServiceUpdatedEvent>().ShouldBe(0);
    }
}
