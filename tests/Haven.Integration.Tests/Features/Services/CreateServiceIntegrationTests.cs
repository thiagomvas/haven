using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Services;

[TestFixture]
[Category("Integration")]
public class CreateServiceIntegrationTests
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
    public async Task CreateService_WithValidInput_CreatesServiceSuccessfully()
    {
        // Arrange - Create project and environment first
        var projectName = "Test Project";
        var projectRequest = new { name = projectName };
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        projectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentName = "staging";
        var environmentRequest = new { name = environmentName, description = "Staging environment" };
        var envResponse = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        envResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        // Act - Create a service
        var serviceName = "web-api";
        var serviceRequest = new CreateServiceCommand
        {
            Name = serviceName,
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig
            {
                Image = "nginx:latest"
            }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        // Assert - HTTP response
        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var response = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.Data.ShouldNotBe(Guid.Empty);

        // Assert - Service was created in database
        var service = await _serviceRepository.GetByIdAsync(response.Data, CancellationToken.None);
        service.ShouldNotBeNull();
        service.Name.ShouldBe(serviceName);
        service.Type.ShouldBe(ServiceType.DockerImage);
        service.ExposureMode.ShouldBe(ExposureMode.External);
        service.Status.ShouldBe(ServiceStatus.Stopped);
        service.EnvironmentId.ShouldBe(environmentId);

        // Assert - Domain event was raised
        _fixture.EventCollector.GetEventCount<ServiceCreatedEvent>().ShouldBe(1);
    }

    [Test]
    public async Task CreateService_WithInternalExposure_CreatesCorrectly()
    {
        // Arrange
        var projectRequest = new { name = "Test Project" };
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "dev" };
        var envResponse = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        // Act - Create an internal service
        var serviceRequest = new CreateServiceCommand
        {
            Name = "database",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.Internal,
            DockerConfig = new DockerConfig
            {
                Image = "postgres:15"
            }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        // Assert
        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var response = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        response.Success.ShouldBeTrue();
        var service = await _serviceRepository.GetByIdAsync(response.Data, CancellationToken.None);

        service.ShouldNotBeNull();
        service.ExposureMode.ShouldBe(ExposureMode.Internal);
        service.Name.ShouldBe("database");
    }

    [Test]
    public async Task CreateMultipleServices_InSameEnvironment_CreatesAllSuccessfully()
    {
        // Arrange - Create project and environment
        var projectRequest = new { name = "Multi Service Project" };
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "production" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        // Act - Create multiple services
        var serviceNames = new[] { "web-api", "worker", "cache" };
        var serviceIds = new List<Guid>();

        foreach (var serviceName in serviceNames)
        {
            var serviceRequest = new CreateServiceCommand()
            {
                Name = serviceName,
                ExposureMode = ExposureMode.Internal,
                Type = ServiceType.DockerImage,
                DockerConfig = new DockerConfig()
                {
                    Image = "nginx"
                }
            };
            var response = await _fixture.Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/environments/{environmentId}/services",
                serviceRequest);

            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            apiResponse.Success.ShouldBeTrue();
            serviceIds.Add(apiResponse.Data);
        }

        // Assert - All services were created
        var services = await _serviceRepository.GetByEnvironmentIdAsync(environmentId, CancellationToken.None);
        services.Count.ShouldBe(3);

        foreach (var serviceId in serviceIds)
        {
            var service = services.FirstOrDefault(s => s.Id == serviceId);
            service.ShouldNotBeNull($"Service {serviceId} was not found in environment");
            service.EnvironmentId.ShouldBe(environmentId);
        }

        // Assert - Event count
        _fixture.EventCollector.GetEventCount<ServiceCreatedEvent>().ShouldBe(3);
    }

    [Test]
    public async Task CreateService_WithInvalidProjectId_ReturnsBadRequest()
    {
        // Act - Try to create service with non-existent project
        var invalidProjectId = Guid.NewGuid();
        var invalidEnvironmentId = Guid.NewGuid();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "test-service",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig
            {
                Image = "nginx"
            }
        };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{invalidProjectId}/environments/{invalidEnvironmentId}/services",
            serviceRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateService_WithInvalidEnvironmentId_ReturnsBadRequest()
    {
        // Arrange - Create project but use invalid environment
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        // Act - Try to create service with non-existent environment
        var serviceRequest = new CreateServiceCommand
        {
            Name = "test-service",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig
            {
                Image = "nginx"
            }
        };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{Guid.NewGuid()}/services",
            serviceRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateService_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange - Create project and environment
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        // Create first service
        var serviceRequest = new CreateServiceCommand
        {
            Name = "web-api",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig
            {
                Image = "nginx"
            }
        };
        var firstResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act - Try to create duplicate service
        var secondResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        // Assert
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task CreateService_WithCaseInsensitiveDuplicateName_ReturnsBadRequest()
    {
        // Arrange - Create project and environment
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        // Create service with one case
        var serviceRequest1 = new CreateServiceCommand
        {
            Name = "WebAPI",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig
            {
                Image = "nginx"
            }
        };
        var firstResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest1);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act - Try to create with different case
        var serviceRequest2 = new CreateServiceCommand
        {
            Name = "webapi",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig
            {
                Image = "nginx"
            }
        };
        var secondResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest2);

        // Assert
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task CreateService_WithReservedName_ReturnsBadRequest()
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

        // Act - Try to create service with reserved name
        var reservedNames = new[] { "haven", "dns", "localhost", "host", "internal" };
        foreach (var reservedName in reservedNames)
        {
            var serviceRequest = new CreateServiceCommand
            {
                Name = reservedName,
                Type = ServiceType.DockerImage,
                ExposureMode = ExposureMode.External,
                DockerConfig = new DockerConfig
                {
                    Image = "nginx"
                }
            };
            var response = await _fixture.Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/environments/{environmentId}/services",
                serviceRequest);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity, $"Reserved name '{reservedName}' should not be allowed");
        }
    }

    [Test]
    public async Task CreateService_UpdatedAtIsSet()
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

        var beforeCreation = DateTime.UtcNow;

        // Act
        var serviceRequest = new CreateServiceCommand
        {
            Name = "test-service",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig
            {
                Image = "nginx"
            }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        var afterCreation = DateTime.UtcNow;
        var apiResponse = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        apiResponse.Success.ShouldBeTrue();
        var service = await _serviceRepository.GetByIdAsync(apiResponse.Data, CancellationToken.None);

        // Assert - CreatedAt and UpdatedAt should be set to approximately now
        service!.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        service.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        service.UpdatedAt.ShouldBe(service.CreatedAt);
    }
}