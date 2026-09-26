using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.AddDomain;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Services;

[TestFixture]
[Category("Integration")]
public class DeleteServiceIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private IProjectRepository _projectRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _projectRepository = _fixture.GetService<IProjectRepository>();
        _serviceRepository = _fixture.GetService<IServiceRepository>();
        _serviceRegistryEntryRepository = _fixture.GetService<IServiceRegistryEntryRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
    }

    private async Task<Guid> CreateServiceAsync(string serviceName)
    {
        var projectRequest = new { name = $"Project-{Guid.NewGuid()}" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 50, CancellationToken.None);
        var projectId = projects.Items.First(p => p.Name == projectRequest.name).Id;

        var environmentRequest = new { name = "staging" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceRequest = new CreateServiceCommand
        {
            Name = serviceName,
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.Internal,
            DockerConfig = new DockerConfig { Image = "nginx" }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);
        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var apiResponse = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        return apiResponse!.Data;
    }

    // Regression test for a bug where deleting a service that had a ServiceRegistryEntry
    // (e.g. from registering a domain) threw a Postgres FK violation
    // (FK_service_registry_services_service_id) because the registry entry was never removed.
    [Test]
    public async Task DeleteService_WithServiceRegistryEntry_SucceedsAndRemovesRegistryEntry()
    {
        var serviceId = await CreateServiceAsync("web-api");

        var domainRequest = new AddDomainCommand { Hostname = "app.example.com", ContainerPort = 8080 };
        var domainResponse = await _fixture.Client.PostAsJsonAsync($"/api/service-registry/{serviceId}/domains", domainRequest);
        domainResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var deleteResponse = await _fixture.Client.DeleteAsync($"/api/services/{serviceId}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var service = await _serviceRepository.GetByIdAsync(serviceId, CancellationToken.None);
        service.ShouldBeNull();

        var registryEntry = await _serviceRegistryEntryRepository.GetForServiceAsync(serviceId, CancellationToken.None);
        registryEntry.ShouldBeNull();
    }
}
