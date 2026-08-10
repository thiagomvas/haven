using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Application.Features.Networks.Queries.ListNetworks;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Integration.Tests.Common;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Haven.Integration.Tests.Features.Networks;

[TestFixture]
[Category("Integration")]
public class NetworkManagementIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private HavenDbContext _dbContext = null!;
    private IProjectRepository _projectRepository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _dbContext = _fixture.GetService<HavenDbContext>();
        _projectRepository = _fixture.GetService<IProjectRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
        _dbContext.Dispose();
    }

    [Test]
    public async Task CreateNetwork_WithSharedType_CreatesAndProvisionsNetwork()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/networks", new CreateNetworkCommand("cloudflared-net"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        created.ShouldNotBeNull();

        var network = await _dbContext.Networks.FirstOrDefaultAsync(n => n.Id == created!.Data);
        network.ShouldNotBeNull();
        network.Type.ShouldBe(NetworkType.Shared);
        network.DockerNetworkId.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task GetNetwork_WithExistingNetwork_ReturnsIt()
    {
        var createResponse = await _fixture.Client.PostAsJsonAsync("/api/networks", new CreateNetworkCommand("shared-net"));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        var response = await _fixture.Client.GetAsync($"/api/networks/{created!.Data}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<NetworkDto>>();
        result!.Data.Name.ShouldBe("shared-net");
    }

    [Test]
    public async Task GetNetwork_WithUnknownId_ReturnsNotFound()
    {
        var response = await _fixture.Client.GetAsync($"/api/networks/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AssignAndUnassignService_ToSharedNetwork_UpdatesMembership()
    {
        var (_, _, serviceId) = await CreateProjectEnvironmentAndService();

        var networkResponse = await _fixture.Client.PostAsJsonAsync("/api/networks", new CreateNetworkCommand("shared-net"));
        var network = await networkResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var networkId = network!.Data;

        var assignResponse = await _fixture.Client.PostAsJsonAsync($"/api/networks/{networkId}/services/{serviceId}", new { });
        assignResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var afterAssign = await _dbContext.ServiceNetworks
            .AnyAsync(sn => sn.ServiceId == serviceId && sn.NetworkId == networkId);
        afterAssign.ShouldBeTrue();

        var unassignResponse = await _fixture.Client.DeleteAsync($"/api/networks/{networkId}/services/{serviceId}");
        unassignResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var afterUnassign = await _dbContext.ServiceNetworks
            .AnyAsync(sn => sn.ServiceId == serviceId && sn.NetworkId == networkId);
        afterUnassign.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteNetwork_WithSharedNetwork_RemovesIt()
    {
        var createResponse = await _fixture.Client.PostAsJsonAsync("/api/networks", new CreateNetworkCommand("shared-net"));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        var deleteResponse = await _fixture.Client.DeleteAsync($"/api/networks/{created!.Data}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stillExists = await _dbContext.Networks.AnyAsync(n => n.Id == created.Data);
        stillExists.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteNetwork_WithProjectEnvironmentNetwork_ReturnsError()
    {
        var (projectId, environmentId, _) = await CreateProjectEnvironmentAndService();

        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var envNetwork = await _dbContext.Networks
            .FirstAsync(n => n.ProjectId == projectId && n.EnvironmentId == environmentId);

        var response = await _fixture.Client.DeleteAsync($"/api/networks/{envNetwork.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        project.ShouldNotBeNull();
    }

    private async Task<(Guid projectId, Guid environmentId, Guid serviceId)> CreateProjectEnvironmentAndService()
    {
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", new { name = "Networked Project" });
        projectResponse.EnsureSuccessStatusCode();

        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First(p => p.Name == "Networked Project").Id;

        var envResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments",
            new { name = "staging" });
        envResponse.EnsureSuccessStatusCode();

        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var serviceRequest = new CreateServiceCommand
        {
            Name = "api",
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.None,
            DockerConfig = new Haven.Domain.ValueObjects.DockerConfig { Image = "nginx:latest" }
        };

        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);
        serviceResponse.EnsureSuccessStatusCode();

        var service = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        return (projectId, environmentId, service!.Data);
    }
}
