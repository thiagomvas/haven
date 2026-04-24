using System.Net;
using System.Net.Http.Json;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Haven.Integration.Tests.Common;
using Shouldly;

namespace Haven.Integration.Tests.Features.Projects;

[TestFixture]
[Category("Integration")]
public class CreateProjectIntegrationTests
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

    [Test]
    public async Task CreateProject_WithValidInput_CreatesProjectAndRaisesEvent()
    {
        // Arrange
        var projectName = "Integration Test Project";

        // Act - Send request to endpoint
        var request = new { name = projectName };
        var response = await _fixture.Client.PostAsJsonAsync("/api/projects", request);

        // Assert - HTTP response
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Assert - Database state
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        projects.TotalCount.ShouldBe(1);
        projects.Items.Count.ShouldBe(1);
        var projectId = projects.Items.First().Id;
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        project.ShouldNotBeNull();
        project.Name.ShouldBe(projectName);

        // Assert - Domain event was raised
        _fixture.EventCollector.GetEventCount<ProjectCreatedEvent>().ShouldBe(1);
    }
}
