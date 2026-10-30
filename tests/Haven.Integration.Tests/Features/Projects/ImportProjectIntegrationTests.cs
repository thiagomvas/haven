using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Projects;

[TestFixture]
[Category("Integration")]
public class ImportProjectIntegrationTests
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
    public void TearDown() => _fixture?.Dispose();

    private async Task<Guid> SeedProjectAsync()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/projects", new { name = "source", alias = "src" });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var projectId = (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;

        var envResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments", new { name = "staging", alias = "stg", description = "Staging" });
        envResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var environmentId = (await _projectRepository.GetByIdAsync(projectId, CancellationToken.None))!.Environments.Single().Id;

        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            new CreateServiceCommand
            {
                Name = "web",
                Type = ServiceType.DockerImage,
                ExposureMode = ExposureMode.External,
                DockerConfig = new DockerConfig { Image = "nginx:latest" }
            });
        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        return projectId;
    }

    private async Task<string> ExportAsync(string url)
    {
        var response = await _fixture.Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<string>>())!.Data!;
    }

    [Test]
    public async Task ImportProject_FullGraph_CreatesProjectEnvironmentsAndServicesWithNewIds()
    {
        var sourceId = await SeedProjectAsync();
        var yaml = await ExportAsync($"/api/projects/{sourceId}/export/manifest?includeEnvironments=true&includeServices=true");

        var response = await _fixture.Client.PostAsJsonAsync("/api/projects/import",
            new { yaml, name = "imported", alias = "imp", includeEnvironments = true, includeServices = true });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var importedId = (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;
        importedId.ShouldNotBe(sourceId);

        var imported = (await _projectRepository.GetByIdAsync(importedId, CancellationToken.None))!;
        imported.Name.ShouldBe("imported");
        var environment = imported.Environments.Single();
        environment.Name.ShouldBe("staging");
        environment.Services.Single().Name.ShouldBe("web");

        var source = (await _projectRepository.GetByIdAsync(sourceId, CancellationToken.None))!;
        environment.Id.ShouldNotBe(source.Environments.Single().Id);
        environment.Services.Single().Id.ShouldNotBe(source.Environments.Single().Services.Single().Id);
    }

    [Test]
    public async Task ImportProject_ProjectOnly_IgnoresEnvironmentsInManifest()
    {
        var sourceId = await SeedProjectAsync();
        var yaml = await ExportAsync($"/api/projects/{sourceId}/export/manifest?includeEnvironments=true&includeServices=true");

        var response = await _fixture.Client.PostAsJsonAsync("/api/projects/import",
            new { yaml, name = "imported", alias = "imp" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var importedId = (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;
        (await _projectRepository.GetByIdAsync(importedId, CancellationToken.None))!.Environments.ShouldBeEmpty();
    }

    [Test]
    public async Task ImportProject_WithoutServices_ImportsEnvironmentsOnly()
    {
        var sourceId = await SeedProjectAsync();
        var yaml = await ExportAsync($"/api/projects/{sourceId}/export/manifest?includeEnvironments=true&includeServices=true");

        var response = await _fixture.Client.PostAsJsonAsync("/api/projects/import",
            new { yaml, name = "imported", alias = "imp", includeEnvironments = true });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var importedId = (await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;
        var imported = (await _projectRepository.GetByIdAsync(importedId, CancellationToken.None))!;
        imported.Environments.Single().Services.ShouldBeEmpty();
    }

    [Test]
    public async Task ImportProject_WithExistingName_ReturnsConflict()
    {
        var sourceId = await SeedProjectAsync();
        var yaml = await ExportAsync($"/api/projects/{sourceId}/export/manifest");

        var response = await _fixture.Client.PostAsJsonAsync("/api/projects/import", new { yaml });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ImportProject_WithInvalidYaml_ReturnsUnprocessableEntity()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/projects/import",
            new { yaml = "name: [unclosed" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task ImportEnvironment_WithServices_AddsEnvironmentToTargetProject()
    {
        var sourceId = await SeedProjectAsync();
        var environmentId = (await _projectRepository.GetByIdAsync(sourceId, CancellationToken.None))!.Environments.Single().Id;
        var yaml = await ExportAsync($"/api/projects/{sourceId}/environments/{environmentId}/export/manifest?includeServices=true");

        var response = await _fixture.Client.PostAsJsonAsync($"/api/projects/{sourceId}/environments/import",
            new { yaml, name = "production", alias = "prd", includeServices = true });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var project = (await _projectRepository.GetByIdAsync(sourceId, CancellationToken.None))!;
        project.Environments.Count.ShouldBe(2);
        project.Environments.Single(e => e.Name == "production").Services.Single().Name.ShouldBe("web");
    }

    [Test]
    public async Task ImportEnvironment_WithoutServices_SkipsServices()
    {
        var sourceId = await SeedProjectAsync();
        var environmentId = (await _projectRepository.GetByIdAsync(sourceId, CancellationToken.None))!.Environments.Single().Id;
        var yaml = await ExportAsync($"/api/projects/{sourceId}/environments/{environmentId}/export/manifest?includeServices=true");

        var response = await _fixture.Client.PostAsJsonAsync($"/api/projects/{sourceId}/environments/import",
            new { yaml, name = "production", alias = "prd" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var project = (await _projectRepository.GetByIdAsync(sourceId, CancellationToken.None))!;
        project.Environments.Single(e => e.Name == "production").Services.ShouldBeEmpty();
    }

    [Test]
    public async Task ImportEnvironment_WithExistingName_ReturnsConflict()
    {
        var sourceId = await SeedProjectAsync();
        var environmentId = (await _projectRepository.GetByIdAsync(sourceId, CancellationToken.None))!.Environments.Single().Id;
        var yaml = await ExportAsync($"/api/projects/{sourceId}/environments/{environmentId}/export/manifest");

        var response = await _fixture.Client.PostAsJsonAsync($"/api/projects/{sourceId}/environments/import",
            new { yaml, alias = "other" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
