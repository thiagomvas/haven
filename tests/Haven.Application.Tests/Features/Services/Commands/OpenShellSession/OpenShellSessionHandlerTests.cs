using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Application.Features.Services.Commands.OpenShellSession;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Commands.OpenShellSession;

[Category("Unit")]
public sealed class OpenShellSessionHandlerTests
{
    private IProjectRepository _projectRepository;
    private IContainerShellService _shellService;
    private OpenShellSessionHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _shellService = Substitute.For<IContainerShellService>();
        _sut = new OpenShellSessionHandler(_projectRepository, _shellService);
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
    public async Task Handle_ShouldNotCreateSession_WhenServiceDoesNotExist()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        command.EnvironmentId = environment.Id;
        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        await _sut.Handle(command, CancellationToken.None);

        await _shellService.DidNotReceive().CreateSessionAsync(Arg.Any<Guid>(), Arg.Any<ShellType>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldDelegateToShellService_WhenServiceExists()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;
        command.ShellType = ShellType.Sh;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var session = Substitute.For<IShellSession>();
        _shellService.CreateSessionAsync(service.Id, ShellType.Sh, Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));

        await _sut.Handle(command, CancellationToken.None);

        await _shellService.Received(1).CreateSessionAsync(service.Id, ShellType.Sh, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldReturnShellServiceResult_WhenServiceExists()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var session = Substitute.For<IShellSession>();
        _shellService.CreateSessionAsync(service.Id, command.ShellType, Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(session);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenShellServiceFails()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);

        _shellService.CreateSessionAsync(service.Id, command.ShellType, Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Failure(Error.Docker.ContainerNotFound));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(Error.Docker.ContainerNotFound);
    }

    private static OpenShellSessionCommand CreateCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceId = Guid.NewGuid(),
        ShellType = ShellType.Bash,
    };
}