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
public class CreateDockerfileServiceTests
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
    public async Task CreateDockerfileService_WithGitSource_CreatesSuccessfully()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "my-git-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "https://github.com/example/repo.git",
                Branch = "main"
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        apiResponse.ShouldNotBeNull();
        apiResponse.Success.ShouldBeTrue();

        var service = await _serviceRepository.GetByIdAsync(apiResponse.Data, CancellationToken.None);
        service.ShouldNotBeNull();
        service.Type.ShouldBe(ServiceType.Dockerfile);

        var config = service.SourceConfig as DockerfileConfig;
        config.ShouldNotBeNull();
        config.Source.ShouldBe(DockerfileSource.Git);
        config.Repository.ShouldBe("https://github.com/example/repo.git");
        config.Branch.ShouldBe("main");
    }

    [Test]
    public async Task CreateDockerfileService_WithGitSourceAndCustomFilePath_PersistsFilePath()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "my-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "https://github.com/example/repo.git",
                Branch = "main",
                FilePath = "docker/Dockerfile.prod"
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var service = await _serviceRepository.GetByIdAsync(apiResponse!.Data, CancellationToken.None);
        var config = service!.SourceConfig as DockerfileConfig;

        config.ShouldNotBeNull();
        config.FilePath.ShouldBe("docker/Dockerfile.prod");
    }

    [Test]
    public async Task CreateDockerfileService_WithGitSourceMissingRepository_ReturnsUnprocessableEntity()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "my-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = string.Empty,
                Branch = "main"
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CreateDockerfileService_WithGitSourceMissingBranch_ReturnsUnprocessableEntity()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "my-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "https://github.com/example/repo.git",
                Branch = string.Empty
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CreateDockerfileService_WithRawSource_CreatesSuccessfully()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();
        var dockerfileContent = "FROM ubuntu:22.04\nRUN echo hello";

        var serviceRequest = new CreateServiceCommand
        {
            Name = "my-raw-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Raw,
                Content = dockerfileContent
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var service = await _serviceRepository.GetByIdAsync(apiResponse!.Data, CancellationToken.None);
        service.ShouldNotBeNull();

        var config = service.SourceConfig as DockerfileConfig;
        config.ShouldNotBeNull();
        config.Source.ShouldBe(DockerfileSource.Raw);
        config.Content.ShouldBe(dockerfileContent);
    }

    [Test]
    public async Task CreateDockerfileService_WithRawSourceMissingContent_ReturnsUnprocessableEntity()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "my-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Raw,
                Content = string.Empty
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task CreateDockerfileService_WithRawSource_AllowsRepositoryField()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "my-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Raw,
                Content = "FROM ubuntu:22.04",
                Repository = "https://github.com/example/repo.git"
            }
        };

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateDockerfileService_RaisesServiceCreatedEvent()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        var serviceRequest = new CreateServiceCommand
        {
            Name = "event-test-service",
            Type = ServiceType.Dockerfile,
            ExposureMode = ExposureMode.Internal,
            DockerfileConfig = new DockerfileConfig
            {
                Source = DockerfileSource.Raw,
                Content = "FROM ubuntu:22.04"
            }
        };

        await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);

        _fixture.EventCollector.GetEventCount<ServiceCreatedEvent>().ShouldBe(1);
    }

    [Test]
    public async Task CreateDockerfileService_WithReservedName_ReturnsUnprocessableEntity()
    {
        var (projectId, environmentId) = await CreateProjectAndEnvironment();

        foreach (var reservedName in new[] { "haven", "dns", "localhost", "host", "internal" })
        {
            var serviceRequest = new CreateServiceCommand
            {
                Name = reservedName,
                Type = ServiceType.Dockerfile,
                ExposureMode = ExposureMode.Internal,
                DockerfileConfig = new DockerfileConfig
                {
                    Source = DockerfileSource.Raw,
                    Content = "FROM ubuntu:22.04"
                }
            };

            var response = await _fixture.Client.PostAsJsonAsync(
                $"/api/projects/{projectId}/environments/{environmentId}/services",
                serviceRequest);

            response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity,
                $"Reserved name '{reservedName}' should not be allowed");
        }
    }

    private async Task<(Guid projectId, Guid environmentId)> CreateProjectAndEnvironment()
    {
        var projectResponse = await _fixture.Client.PostAsJsonAsync("/api/projects", new { name = "Test Project" });
        projectResponse.EnsureSuccessStatusCode();

        var projects = await _projectRepository.GetPagedAsync(1, 10, CancellationToken.None);
        var projectId = projects.Items.First().Id;

        var envResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments",
            new { name = "staging" });
        envResponse.EnsureSuccessStatusCode();

        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        var environmentId = project!.Environments.First().Id;

        return (projectId, environmentId);
    }
}