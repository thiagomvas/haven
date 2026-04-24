using System.Net;
using System.Net.Http.Json;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Haven.Infrastructure.Persistence;
using Haven.Integration.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Haven.Integration.Tests.Features.Projects;

[TestFixture]
public class UpdateProjectIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private IProjectRepository _projectRepository = null!;
    private HavenDbContext _dbContext = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _projectRepository = _fixture.GetService<IProjectRepository>();
        _dbContext = _fixture.GetService<HavenDbContext>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
        _dbContext.Dispose();
    }

    [Test]
    public async Task UpdateProject_PartialUpdate_UpdatesName()
    {
        // Arrange - Create project
        var createRequest = new { name = "Original Project", description = "Original desc" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", createRequest);

        var projects = await _projectRepository.GetPagedAsync(1, 100, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        _fixture.EventCollector.Clear();

        // Act - Update only name
        var updateRequest = new { name = "Updated Project" };
        var response = await _fixture.Client.PatchAsJsonAsync($"/api/projects/{projectId}", updateRequest);

        // Assert - HTTP response
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert - Event was persisted
        _fixture.EventCollector.GetEventCount<ProjectUpdatedEvent>().ShouldBe(1);

        // Assert - Database was updated (clear EF cache first)
        _dbContext.ChangeTracker.Clear();
        var updated = await _dbContext.Projects.AsNoTracking().FirstAsync(p => p.Id == projectId);
        updated.Name.ShouldBe("Updated Project");
        updated.Description.ShouldBe("Original desc"); // Unchanged
    }

    [Test]
    public async Task UpdateProject_AddDescription_UpdatesDescription()
    {
        // Arrange
        var createRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", createRequest);

        var projects = await _projectRepository.GetPagedAsync(1, 100, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        _fixture.EventCollector.Clear();

        // Act - Add description
        var updateRequest = new { description = "Added description" };
        var response = await _fixture.Client.PatchAsJsonAsync($"/api/projects/{projectId}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _fixture.EventCollector.GetEventCount<ProjectUpdatedEvent>().ShouldBe(1);

        // Assert - Database was updated
        _dbContext.ChangeTracker.Clear();
        var updated = await _dbContext.Projects.AsNoTracking().FirstAsync(p => p.Id == projectId);
        updated.Description.ShouldBe("Added description");
        updated.Name.ShouldBe("Test Project"); // Unchanged
    }
}
