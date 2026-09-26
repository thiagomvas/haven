using System.Net;
using System.Net.Http.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceTemplates.Commands.CreateServiceFromTemplate;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.ServiceTemplates;

[TestFixture]
[Category("Integration")]
public class CreateServiceFromTemplateIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;
    private IProjectRepository _projectRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IEnvironmentVariableRepository _environmentVariableRepository = null!;
    private ISecretVariableRepository _secretVariableRepository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _projectRepository = _fixture.GetService<IProjectRepository>();
        _serviceRepository = _fixture.GetService<IServiceRepository>();
        _environmentVariableRepository = _fixture.GetService<IEnvironmentVariableRepository>();
        _secretVariableRepository = _fixture.GetService<ISecretVariableRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
    }

    [Test]
    public async Task CreateServiceFromTemplate_WithPostgresTemplate_CreatesServiceWithEnvVarsAndSecrets()
    {
        // Arrange - Create project and environment first
        var projectRequest = new { name = "Test Project" };
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        projectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        var envResponse = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        envResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        // Act - Create a service from the built-in postgres template, supplying only the required password
        var templateRequest = new CreateServiceFromTemplateCommand
        {
            InputValues = new Dictionary<string, string>
            {
                { "postgres_password", "s3cret" }
            }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/from-template/postgres",
            templateRequest);

        // Assert - HTTP response
        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var response = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.Data.ShouldNotBe(Guid.Empty);

        // Assert - Service was created with the resolved Docker image (defaults applied for version/user/database)
        var service = await _serviceRepository.GetByIdAsync(response.Data, CancellationToken.None);
        service.ShouldNotBeNull();
        service.Name.ShouldBe("PostgreSQL");
        service.Type.ShouldBe(ServiceType.DockerImage);
        service.SourceConfig.ShouldBeOfType<DockerConfig>();
        ((DockerConfig)service.SourceConfig!).Image.ShouldBe("postgres:17-alpine");

        // Assert - Non-secret env vars were persisted with resolved defaults
        var envVars = (await _environmentVariableRepository.GetForServiceAsync(service.Id, CancellationToken.None)).ToList();
        envVars.Single(v => v.Key == "POSTGRES_USER").Value.ShouldBe("postgres");
        envVars.Single(v => v.Key == "POSTGRES_DB").Value.ShouldBe("postgres");

        // Assert - Secret was persisted with the supplied value
        var secrets = (await _secretVariableRepository.GetForParentAsync(service.Id, EnvironmentVariableParentType.Service, CancellationToken.None)).ToList();
        var passwordSecret = secrets.Single(s => s.Key == "POSTGRES_PASSWORD");
        passwordSecret.Value.ShouldNotBeNull();
        passwordSecret.Value!.Value.ShouldBe("s3cret");
    }

    [Test]
    public async Task CreateServiceFromTemplate_WithRedisTemplate_CreatesServiceSuccessfully()
    {
        // Regression test: the real embedded redis.yaml was previously missing its `container`
        // section entirely, which crashed the whole request with an unhandled NullReferenceException
        // from ServiceTemplateInstantiator.ResolveVariables instead of a clean error response.
        var projectRequest = new { name = "Test Project" };
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        projectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        var envResponse = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        envResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var templateRequest = new CreateServiceFromTemplateCommand
        {
            InputValues = new Dictionary<string, string>
            {
                { "redis_password", "s3cret" }
            },
            ExposureMode = ExposureMode.External,
            Ports = ["6379:6379"]
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/from-template/redis",
            templateRequest);

        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var response = await serviceResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();

        var service = await _serviceRepository.GetByIdAsync(response.Data, CancellationToken.None);
        service.ShouldNotBeNull();
        service.Name.ShouldBe("Redis");
        service.ExposureMode.ShouldBe(ExposureMode.External);
        service.SourceConfig.ShouldBeOfType<DockerConfig>();
        var dockerConfig = (DockerConfig)service.SourceConfig!;
        dockerConfig.Image.ShouldBe("redis:7-alpine");
        dockerConfig.Ports.ShouldBe(["6379:6379"]);

        var secrets = (await _secretVariableRepository.GetForParentAsync(service.Id, EnvironmentVariableParentType.Service, CancellationToken.None)).ToList();
        var passwordSecret = secrets.Single(s => s.Key == "REDIS_PASSWORD");
        passwordSecret.Value!.Value.ShouldBe("s3cret");
    }

    [Test]
    public async Task CreateServiceFromTemplate_WhenRequiredInputMissing_ReturnsValidationError()
    {
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var templateRequest = new CreateServiceFromTemplateCommand
        {
            InputValues = new Dictionary<string, string>()
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/from-template/postgres",
            templateRequest);

        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CreateServiceFromTemplate_WhenTemplateDoesNotExist_ReturnsNotFound()
    {
        var projectRequest = new { name = "Test Project" };
        await _fixture.Client.PostAsJsonAsync("/api/projects", projectRequest);
        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var environmentRequest = new { name = "staging" };
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", environmentRequest);
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        var templateRequest = new CreateServiceFromTemplateCommand
        {
            InputValues = new Dictionary<string, string> { { "postgres_password", "s3cret" } }
        };
        var serviceResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/from-template/does-not-exist",
            templateRequest);

        serviceResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
