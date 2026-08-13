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

namespace Haven.Integration.Tests.Features.ServiceRegistry;

[TestFixture]
[Category("Integration")]
public class AddDomainIntegrationTests
{
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

    [Test]
    public async Task AddDomain_ToNeverDeployedService_CreatesRegistryEntryAndDomain()
    {
        var serviceId = await CreateServiceAsync("web-api");

        var request = new AddDomainCommand { Hostname = "app.example.com", ContainerPort = 8080 };
        var response = await _fixture.Client.PostAsJsonAsync($"/api/service-registry/{serviceId}/domains", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var entry = await _serviceRegistryEntryRepository.GetForServiceAsync(serviceId, CancellationToken.None);
        entry.ShouldNotBeNull();
        entry.Domains.ShouldHaveSingleItem();
        entry.Domains.First().Hostname.ShouldBe("app.example.com");
        entry.Domains.First().ContainerPort.ShouldBe(8080);
    }

    [Test]
    public async Task AddDomain_DuplicateHostnameAcrossDifferentServices_ReturnsConflict()
    {
        var firstServiceId = await CreateServiceAsync("web-api");
        var secondServiceId = await CreateServiceAsync("worker");

        var firstRequest = new AddDomainCommand { Hostname = "shared.example.com", ContainerPort = 8080 };
        var firstResponse = await _fixture.Client.PostAsJsonAsync($"/api/service-registry/{firstServiceId}/domains", firstRequest);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var secondRequest = new AddDomainCommand { Hostname = "shared.example.com", ContainerPort = 3000 };
        var secondResponse = await _fixture.Client.PostAsJsonAsync($"/api/service-registry/{secondServiceId}/domains", secondRequest);

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task AddDomain_WithInvalidHostname_ReturnsBadRequest()
    {
        var serviceId = await CreateServiceAsync("web-api");

        var request = new AddDomainCommand { Hostname = "not a hostname!!", ContainerPort = 8080 };
        var response = await _fixture.Client.PostAsJsonAsync($"/api/service-registry/{serviceId}/domains", request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}