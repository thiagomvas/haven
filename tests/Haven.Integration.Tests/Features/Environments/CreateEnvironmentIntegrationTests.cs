using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Events;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Environments;

[TestFixture]
[Category("Integration")]
public class CreateEnvironmentIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private IProjectRepository _projectRepository = null!;
    private INetworkRepository _networkRepository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _projectRepository = _fixture.GetService<IProjectRepository>();
        _networkRepository = _fixture.GetService<INetworkRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
    }

    [Test]
    public async Task CreateEnvironment_WithValidInput_CreatesEnvironmentAndNetwork()
    {
        // Arrange - Create a project first
        var projectName = "Test Project";
        var projectRequest = new { name = projectName };
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        projectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        // Act - Create an environment
        var environmentName = "staging";
        var environmentRequest = new { name = environmentName, description = "Staging environment" };
        var envResponse = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);

        // Assert - HTTP response
        envResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Assert - Environment was created in database
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        project.ShouldNotBeNull();
        project.Environments.Count.ShouldBe(1);
        var environment = project.Environments[0];
        environment.Name.ShouldBe(environmentName);

        // Assert - Network was automatically created with correct properties
        var networks = await _networkRepository.GetByProjectAndEnvironmentAsync(projectId, environment.Id, CancellationToken.None);
        networks.Count.ShouldBe(1);

        var network = networks[0];
        network.ProjectId.ShouldBe(projectId);
        network.EnvironmentId.ShouldBe(environment.Id);
        network.Type.ShouldBe(Haven.Domain.NetworkType.ProjectEnvironment);
        network.Name.ShouldNotBeNullOrWhiteSpace();

        // Assert - Domain event was raised
        _fixture.EventCollector.GetEventCount<EnvironmentCreatedEvent>().ShouldBe(1);
    }

    [Test]
    public async Task CreateMultipleEnvironments_CreatesNetworkForEach()
    {
        // Arrange - Create a project
        var projectName = "Multi Env Project";
        var projectRequest = new { name = projectName };
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        // Act - Create multiple environments
        var environments = new[] { "dev", "staging", "production" };
        foreach (var envName in environments)
        {
            var envRequest = new { name = envName };
            var response = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", envRequest);
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        // Assert - A network was created for each environment
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        project.Environments.Count.ShouldBe(3);

        foreach (var environment in project.Environments)
        {
            var networks = await _networkRepository.GetByProjectAndEnvironmentAsync(projectId, environment.Id, CancellationToken.None);
            networks.Count.ShouldBe(1, $"Expected one network for environment '{environment.Name}'");
        }
    }
}