using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Services;
using NSubstitute;
using Shouldly;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class EnvironmentVariableServiceTests
{
    private EnvironmentVariableService _sut = null!;
    private IServiceRepository _serviceRepository = null!;
    private IEnvironmentVariableRepository _envVarRepository = null!;

    [SetUp]
    public void Setup()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _envVarRepository = Substitute.For<IEnvironmentVariableRepository>();
        _sut = new EnvironmentVariableService(_serviceRepository, _envVarRepository);

        _envVarRepository.GetForProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _envVarRepository.GetForEnvironmentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _envVarRepository.GetForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenServiceNotFound_ShouldReturnEmpty()
    {
        _serviceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Service?)null);

        var result = await _sut.BuildVariablesForServiceAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test(Description = "Returns empty when the service has no linked environment")]
    public async Task BuildVariablesForServiceAsync_WhenServiceHasNoEnvironment_ShouldReturnEmpty()
    {
        var service = CreateService(withEnvironment: false);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenEnvironmentHasNoProject_ShouldReturnEmpty()
    {
        var service = CreateService(withProject: false);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenNoVariablesAtAnyLevel_ShouldReturnEmpty()
    {
        var service = CreateService();
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenOnlyProjectVariables_ShouldReturnThem()
    {
        var service = CreateService();
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _envVarRepository.GetForProjectAsync(service.Environment!.Project!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "project-value")]);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldHaveSingleItem();
        result.Single().Value.ShouldBe("project-value");
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenEnvironmentOverridesProject_ShouldReturnEnvironmentValue()
    {
        var service = CreateService();
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _envVarRepository.GetForProjectAsync(service.Environment!.Project!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "project-value")]);
        _envVarRepository.GetForEnvironmentAsync(service.Environment!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "env-value")]);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldHaveSingleItem();
        result.Single().Value.ShouldBe("env-value");
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenServiceOverridesEnvironment_ShouldReturnServiceValue()
    {
        var service = CreateService();
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _envVarRepository.GetForEnvironmentAsync(service.Environment!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "env-value")]);
        _envVarRepository.GetForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "service-value")]);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldHaveSingleItem();
        result.Single().Value.ShouldBe("service-value");
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenServiceOverridesProject_ShouldReturnServiceValue()
    {
        var service = CreateService();
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _envVarRepository.GetForProjectAsync(service.Environment!.Project!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "project-value")]);
        _envVarRepository.GetForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "service-value")]);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldHaveSingleItem();
        result.Single().Value.ShouldBe("service-value");
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenVariablesAtAllLevels_ShouldMergeAllWithCorrectPriority()
    {
        var service = CreateService();
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _envVarRepository.GetForProjectAsync(service.Environment!.Project!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("PROJECT_ONLY", "p"), Var("SHARED", "project-value")]);
        _envVarRepository.GetForEnvironmentAsync(service.Environment!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("ENV_ONLY", "e"), Var("SHARED", "env-value")]);
        _envVarRepository.GetForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([Var("SERVICE_ONLY", "s")]);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);
        var dict = result.ToDictionary(x => x.Key, x => x.Value);

        dict.Count.ShouldBe(4);
        dict["PROJECT_ONLY"].ShouldBe("p");
        dict["ENV_ONLY"].ShouldBe("e");
        dict["SERVICE_ONLY"].ShouldBe("s");
        dict["SHARED"].ShouldBe("env-value");
    }

    [Test]
    public async Task BuildVariablesForServiceAsync_WhenAllLevelsOverrideSameKey_ServiceValueShouldWin()
    {
        var service = CreateService();
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _envVarRepository.GetForProjectAsync(service.Environment!.Project!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "project-value")]);
        _envVarRepository.GetForEnvironmentAsync(service.Environment!.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "env-value")]);
        _envVarRepository.GetForServiceAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([Var("KEY", "service-value")]);

        var result = await _sut.BuildVariablesForServiceAsync(service.Id, CancellationToken.None);

        result.ShouldHaveSingleItem();
        result.Single().Value.ShouldBe("service-value");
    }

    private static Service CreateService(bool withEnvironment = true, bool withProject = true)
    {
        var project = withProject ? Project.Create("test-project") : null;
        var environment = withEnvironment ? project?.AddEnvironment("dev") : null;
        var service = environment is not null
            ? project!.AddService(environment.Id, "test-svc", ServiceType.DockerImage, ExposureMode.Internal,
                new DockerConfig { Image = "myapp:latest" })
            : Service.Create(Guid.NewGuid(), "test-svc", ServiceType.DockerImage, ExposureMode.Internal,
                new DockerConfig { Image = "myapp:latest" });

        if (withEnvironment && environment is not null)
            service.Environment = environment;

        return service;
    }

    private static EnvironmentVariables Var(string key, string? value) =>
        new() { Key = key, Value = value };
}
