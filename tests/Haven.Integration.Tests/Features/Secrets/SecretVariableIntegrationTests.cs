using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Secrets;
using Haven.Application.Features.Secrets.Commands.CreateSecretVariable;
using Haven.Application.Features.Secrets.Commands.UpdateSecretVariable;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Features.Secrets;

[TestFixture]
[Category("Integration")]
public class SecretVariableIntegrationTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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

    private async Task<Guid> CreateProjectAsync(string name = "Test Project")
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/projects", new { name });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var projects = await _projectRepository.GetPagedAsync(1, 50, CancellationToken.None);
        return projects.Items.First(p => p.Name == name).Id;
    }

    private async Task<Guid> CreateEnvironmentAsync(Guid projectId, string name = "staging")
    {
        await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/environments", new { name });
        var project = await _projectRepository.GetByIdAsync(projectId, CancellationToken.None);
        return project!.Environments.First(e => e.Name == name).Id;
    }

    private async Task<Guid> CreateServiceAsync(Guid projectId, Guid environmentId, string name = "web")
    {
        var serviceRequest = new CreateServiceCommand
        {
            Name = name,
            Type = ServiceType.DockerImage,
            ExposureMode = ExposureMode.External,
            DockerConfig = new DockerConfig { Image = "nginx:latest" }
        };
        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services",
            serviceRequest);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        return apiResponse!.Data;
    }

    [Test]
    public async Task CreateProjectSecret_WithValidInput_CreatesSecretSuccessfully()
    {
        var projectId = await CreateProjectAsync();

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/secrets",
            new CreateSecretVariableCommand { Key = "API_KEY", Value = "super-secret" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        apiResponse!.Success.ShouldBeTrue();
        apiResponse.Data.ShouldNotBe(Guid.Empty);

        var getResponse = await _fixture.Client.GetAsync($"/api/secrets/{apiResponse.Data}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secret = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SecretVariableDto>>(ResponseJsonOptions);
        secret!.Data.Key.ShouldBe("API_KEY");
        secret.Data.ParentId.ShouldBe(projectId);
        secret.Data.ParentType.ShouldBe(EnvironmentVariableParentType.Project);
        secret.Data.HasValue.ShouldBeTrue();
    }

    [Test]
    public async Task CreateProjectSecret_WithDuplicateKey_ReturnsConflict()
    {
        var projectId = await CreateProjectAsync();
        var request = new CreateSecretVariableCommand { Key = "API_KEY", Value = "super-secret" };

        var first = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/secrets", request);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await _fixture.Client.PostAsJsonAsync($"/api/projects/{projectId}/secrets", request);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task GetProjectSecrets_ReturnsSecretsForThatProjectOnly()
    {
        var projectId = await CreateProjectAsync("Project A");
        var otherProjectId = await CreateProjectAsync("Project B");

        await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/secrets",
            new CreateSecretVariableCommand { Key = "KEY_A", Value = "value-a" });
        await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{otherProjectId}/secrets",
            new CreateSecretVariableCommand { Key = "KEY_B", Value = "value-b" });

        var response = await _fixture.Client.GetAsync($"/api/projects/{projectId}/secrets");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<SecretVariableDto>>(ResponseJsonOptions);
        paged!.Items.Count.ShouldBe(1);
        paged.Items[0].Key.ShouldBe("KEY_A");
    }

    [Test(Description = "The merged create endpoint resolves ParentId/ParentType from the environment-scoped route")]
    public async Task CreateEnvironmentSecret_WithValidInput_CreatesSecretSuccessfully()
    {
        var projectId = await CreateProjectAsync();
        var environmentId = await CreateEnvironmentAsync(projectId);

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/secrets",
            new CreateSecretVariableCommand { Key = "API_KEY", Value = "super-secret" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        var getResponse = await _fixture.Client.GetAsync($"/api/secrets/{apiResponse!.Data}");
        var secret = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SecretVariableDto>>(ResponseJsonOptions);
        secret!.Data.ParentId.ShouldBe(environmentId);
        secret.Data.ParentType.ShouldBe(EnvironmentVariableParentType.Environment);
    }

    [Test(Description = "The merged create endpoint resolves ParentId/ParentType from the service-scoped route, even though projectId and environmentId are also present in that route")]
    public async Task CreateServiceSecret_WithValidInput_CreatesSecretSuccessfully()
    {
        var projectId = await CreateProjectAsync();
        var environmentId = await CreateEnvironmentAsync(projectId);
        var serviceId = await CreateServiceAsync(projectId, environmentId);

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}/secrets",
            new CreateSecretVariableCommand { Key = "API_KEY", Value = "super-secret" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        var getResponse = await _fixture.Client.GetAsync($"/api/secrets/{apiResponse!.Data}");
        var secret = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SecretVariableDto>>(ResponseJsonOptions);
        secret!.Data.ParentId.ShouldBe(serviceId);
        secret.Data.ParentType.ShouldBe(EnvironmentVariableParentType.Service);
    }

    [Test(Description = "The merged list endpoint resolves ParentId/ParentType from the service-scoped route")]
    public async Task GetServiceSecrets_ReturnsSecretsForThatServiceOnly()
    {
        var projectId = await CreateProjectAsync();
        var environmentId = await CreateEnvironmentAsync(projectId);
        var serviceId = await CreateServiceAsync(projectId, environmentId, "web");
        var otherServiceId = await CreateServiceAsync(projectId, environmentId, "api");

        await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}/secrets",
            new CreateSecretVariableCommand { Key = "KEY_A", Value = "value-a" });
        await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{otherServiceId}/secrets",
            new CreateSecretVariableCommand { Key = "KEY_B", Value = "value-b" });

        var response = await _fixture.Client.GetAsync(
            $"/api/projects/{projectId}/environments/{environmentId}/services/{serviceId}/secrets");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<SecretVariableDto>>(ResponseJsonOptions);
        paged!.Items.Count.ShouldBe(1);
        paged.Items[0].Key.ShouldBe("KEY_A");
    }

    [Test]
    public async Task UpdateSecret_WithNewValue_UpdatesSuccessfully()
    {
        var projectId = await CreateProjectAsync();
        var createResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/secrets",
            new CreateSecretVariableCommand { Key = "API_KEY", Value = "old-value" });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        var updateResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/secrets/{created!.Data}",
            new UpdateSecretVariableCommand { Key = "RENAMED_KEY" },
            _fixture.JsonSerializerOptions);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await _fixture.Client.GetAsync($"/api/secrets/{created.Data}");
        var secret = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SecretVariableDto>>(ResponseJsonOptions);
        secret!.Data.Key.ShouldBe("RENAMED_KEY");
    }

    [Test]
    public async Task UpdateSecret_ThatDoesNotExist_ReturnsNotFound()
    {
        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/secrets/{Guid.NewGuid()}",
            new UpdateSecretVariableCommand { Key = "NEW_KEY" },
            _fixture.JsonSerializerOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteSecret_RemovesIt()
    {
        var projectId = await CreateProjectAsync();
        var createResponse = await _fixture.Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/secrets",
            new CreateSecretVariableCommand { Key = "API_KEY", Value = "value" });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        var deleteResponse = await _fixture.Client.DeleteAsync($"/api/secrets/{created!.Data}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await _fixture.Client.GetAsync($"/api/secrets/{created.Data}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteSecret_ThatDoesNotExist_ReturnsNotFound()
    {
        var response = await _fixture.Client.DeleteAsync($"/api/secrets/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}