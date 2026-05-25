using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Services.Commands.RestartService;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.ValueObjects;
using NSubstitute;
using Shouldly;

namespace Haven.Application.Tests.Features.Services.Commands.RestartService;

[Category("Unit")]
public sealed class RestartServiceHandlerTests
{
    private IProjectRepository _projectRepository;
    private IDeploymentOrchestrator _deploymentOrchestrator;
    private RestartServiceHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _deploymentOrchestrator = Substitute.For<IDeploymentOrchestrator>();
        _sut = new RestartServiceHandler(_projectRepository, _deploymentOrchestrator);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var command = CreateCommand();
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenEnvironmentDoesNotExist()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenServiceDoesNotExist()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenOrchestratorReturnsFailure()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deploymentOrchestrator.RestartServiceAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result>(Error.Validation));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldCallRestartServiceAsync_OnOrchestrator()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deploymentOrchestrator.RestartServiceAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        await _sut.Handle(command, CancellationToken.None);

        await _deploymentOrchestrator.Received(1).RestartServiceAsync(service, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnSuccess()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deploymentOrchestrator.RestartServiceAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    private static RestartServiceCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceId = Guid.NewGuid(),
    };
}
