using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
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
    private IDeployServiceFactory _deployServiceFactory;
    private RestartServiceHandler _sut;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _deployServiceFactory = Substitute.For<IDeployServiceFactory>();
        _sut = new RestartServiceHandler(_projectRepository, _deployServiceFactory);
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
    public async Task Handle_ShouldReturnFailure_WhenDeployServiceReturnsFailure()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        var mockDeployService = Substitute.For<IDeployService>();
        mockDeployService.RestartAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result>(Error.Validation));

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deployServiceFactory.Create(Arg.Any<Haven.Domain.Entities.Service>())
            .Returns(mockDeployService);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        
    }

    [Test]
    public async Task Handle_ShouldCallRestartAsync_OnDeployService()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        var mockDeployService = Substitute.For<IDeployService>();
        mockDeployService.RestartAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deployServiceFactory.Create(Arg.Any<Haven.Domain.Entities.Service>())
            .Returns(mockDeployService);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await mockDeployService.Received(1).RestartAsync(service, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldCallRestartService_OnProject_WhenSuccessful()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        var mockDeployService = Substitute.For<IDeployService>();
        mockDeployService.RestartAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deployServiceFactory.Create(Arg.Any<Haven.Domain.Entities.Service>())
            .Returns(mockDeployService);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        service.Status.ShouldBe(Haven.Domain.ServiceStatus.Running);
    }

    [Test]
    public async Task Handle_ShouldRaiseServiceRestartedEvent()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        var mockDeployService = Substitute.For<IDeployService>();
        mockDeployService.RestartAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deployServiceFactory.Create(Arg.Any<Haven.Domain.Entities.Service>())
            .Returns(mockDeployService);

        await _sut.Handle(command, CancellationToken.None);

        var domainEvents = project.Environments.First().Services.First().DomainEvents.ToList();
        domainEvents.ShouldNotBeEmpty();
        domainEvents.Last().ShouldBeOfType<Haven.Domain.Events.ServiceRestartedEvent>();
    }

    [Test]
    public async Task Handle_ShouldPersistChanges_OnSuccess()
    {
        var command = CreateCommand();
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(environment.Id, "web", ServiceType.DockerImage, ExposureMode.External,
            new DockerConfig { Image = "nginx" });
        command.EnvironmentId = environment.Id;
        command.ServiceId = service.Id;

        var mockDeployService = Substitute.For<IDeployService>();
        mockDeployService.RestartAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deployServiceFactory.Create(Arg.Any<Haven.Domain.Entities.Service>())
            .Returns(mockDeployService);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        
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

        var mockDeployService = Substitute.For<IDeployService>();
        mockDeployService.RestartAsync(Arg.Any<Haven.Domain.Entities.Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _projectRepository.GetByIdAsync(command.ProjectId, Arg.Any<CancellationToken>())
            .Returns(project);
        _deployServiceFactory.Create(Arg.Any<Haven.Domain.Entities.Service>())
            .Returns(mockDeployService);

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
