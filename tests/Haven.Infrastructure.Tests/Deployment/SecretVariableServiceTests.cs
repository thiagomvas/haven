using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[TestFixture]
[Category("Unit")]
public sealed class SecretVariableServiceTests
{
    private SecretVariableService _sut = null!;
    private IEnvironmentRepository _environmentRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private ISecretVariableRepository _secretVariableRepository = null!;

    [SetUp]
    public void Setup()
    {
        _environmentRepository = Substitute.For<IEnvironmentRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _secretVariableRepository = Substitute.For<ISecretVariableRepository>();
        _sut = new SecretVariableService(_environmentRepository, _serviceRepository, _secretVariableRepository);

        _secretVariableRepository
            .GetForParentAsync(Arg.Any<Guid>(), Arg.Any<EnvironmentVariableParentType>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Test]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WhenServiceNotFound_ShouldReturnEmpty()
    {
        _serviceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Service?)null);

        var result = await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(Guid.NewGuid());

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WhenServiceHasNoEnvironment_ShouldReturnEmpty()
    {
        var service = Service.Create(Guid.NewGuid(), "test-svc", ServiceType.DockerImage, ExposureMode.Internal,
            sourceConfig: new DockerConfig { Image = "myapp:latest" });
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id);

        result.ShouldBeEmpty();
    }

    [Test(Description = "A secret with a value round-trips through encryption/decryption transparently")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WithValue_ReturnsDecryptedValue()
    {
        var service = CreateService();
        StubHierarchy(service);
        _secretVariableRepository
            .GetForParentAsync(service.Id, EnvironmentVariableParentType.Service, Arg.Any<CancellationToken>())
            .Returns([Secret("API_KEY", "super-secret")]);

        var result = (await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id)).ToList();

        result.ShouldHaveSingleItem();
        result[0].Key.ShouldBe("API_KEY");
        result[0].Value.ShouldBe("super-secret");
    }

    [Test(Description = "A secret with no value stored must not throw and should surface as empty, not attempt to re-decrypt or re-encrypt an empty string")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WithNullValue_ReturnsEmptyStringWithoutThrowing()
    {
        var service = CreateService();
        StubHierarchy(service);
        _secretVariableRepository
            .GetForParentAsync(service.Id, EnvironmentVariableParentType.Service, Arg.Any<CancellationToken>())
            .Returns([Secret("UNSET_KEY", null)]);

        var result = (await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id)).ToList();

        result.ShouldHaveSingleItem();
        result[0].Key.ShouldBe("UNSET_KEY");
        result[0].Value.ShouldBe(string.Empty);
    }

    [Test(Description = "A project-scoped secret cascades down to services under that project")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WhenOnlyProjectSecrets_ShouldReturnThem()
    {
        var service = CreateService();
        StubHierarchy(service);
        _secretVariableRepository
            .GetForParentAsync(service.Environment!.ProjectId, EnvironmentVariableParentType.Project, Arg.Any<CancellationToken>())
            .Returns([Secret("KEY", "project-value")]);

        var result = (await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id)).ToList();

        result.ShouldHaveSingleItem();
        result[0].Value.ShouldBe("project-value");
    }

    [Test(Description = "An environment-scoped secret overrides a project-scoped secret with the same key")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WhenEnvironmentOverridesProject_ShouldReturnEnvironmentValue()
    {
        var service = CreateService();
        StubHierarchy(service);
        _secretVariableRepository
            .GetForParentAsync(service.Environment!.ProjectId, EnvironmentVariableParentType.Project, Arg.Any<CancellationToken>())
            .Returns([Secret("KEY", "project-value")]);
        _secretVariableRepository
            .GetForParentAsync(service.Environment!.Id, EnvironmentVariableParentType.Environment, Arg.Any<CancellationToken>())
            .Returns([Secret("KEY", "env-value")]);

        var result = (await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id)).ToList();

        result.ShouldHaveSingleItem();
        result[0].Value.ShouldBe("env-value");
    }

    [Test(Description = "A service-scoped secret overrides an environment-scoped secret with the same key")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WhenServiceOverridesEnvironment_ShouldReturnServiceValue()
    {
        var service = CreateService();
        StubHierarchy(service);
        _secretVariableRepository
            .GetForParentAsync(service.Environment!.Id, EnvironmentVariableParentType.Environment, Arg.Any<CancellationToken>())
            .Returns([Secret("KEY", "env-value")]);
        _secretVariableRepository
            .GetForParentAsync(service.Id, EnvironmentVariableParentType.Service, Arg.Any<CancellationToken>())
            .Returns([Secret("KEY", "service-value")]);

        var result = (await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id)).ToList();

        result.ShouldHaveSingleItem();
        result[0].Value.ShouldBe("service-value");
    }

    [Test(Description = "Secrets at every scope are merged, with the most specific scope winning on key collisions")]
    public async Task GetSecretsAsEnvironmentVariablesForServiceAsync_WhenSecretsAtAllLevels_ShouldMergeAllWithCorrectPriority()
    {
        var service = CreateService();
        StubHierarchy(service);
        _secretVariableRepository
            .GetForParentAsync(service.Environment!.ProjectId, EnvironmentVariableParentType.Project, Arg.Any<CancellationToken>())
            .Returns([Secret("PROJECT_ONLY", "p"), Secret("SHARED", "project-value")]);
        _secretVariableRepository
            .GetForParentAsync(service.Environment!.Id, EnvironmentVariableParentType.Environment, Arg.Any<CancellationToken>())
            .Returns([Secret("ENV_ONLY", "e"), Secret("SHARED", "env-value")]);
        _secretVariableRepository
            .GetForParentAsync(service.Id, EnvironmentVariableParentType.Service, Arg.Any<CancellationToken>())
            .Returns([Secret("SERVICE_ONLY", "s")]);

        var result = await _sut.GetSecretsAsEnvironmentVariablesForServiceAsync(service.Id);
        var dict = result.ToDictionary(x => x.Key, x => x.Value);

        dict.Count.ShouldBe(4);
        dict["PROJECT_ONLY"].ShouldBe("p");
        dict["ENV_ONLY"].ShouldBe("e");
        dict["SERVICE_ONLY"].ShouldBe("s");
        dict["SHARED"].ShouldBe("env-value");
    }

    private void StubHierarchy(Service service)
    {
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _environmentRepository.GetByIdAsync(service.Environment!.Id, Arg.Any<CancellationToken>())
            .Returns(service.Environment);
    }

    private static Service CreateService()
    {
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("dev");
        var service = project.AddService(environment.Id, "test-svc", ServiceType.DockerImage, ExposureMode.Internal,
            null, new DockerConfig { Image = "myapp:latest" });
        service.Environment = environment;

        return service;
    }

    private static SecretVariable Secret(string key, string? value) => new()
    {
        Key = key,
        Value = value is null ? null : EncryptedValue.From(value)
    };
}