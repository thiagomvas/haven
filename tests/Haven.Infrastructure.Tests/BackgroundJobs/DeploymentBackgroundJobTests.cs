using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class DeploymentBackgroundJobTests
{
    private IProjectRepository _projectRepository = null!;
    private IDeploymentOrchestrator _orchestrator = null!;
    private IDeploymentCancellationService _cancellationService = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ILogger<DeploymentBackgroundJob> _logger = null!;
    private DeploymentBackgroundJob _sut = null!;

    [SetUp]
    public void Setup()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _orchestrator = Substitute.For<IDeploymentOrchestrator>();
        _cancellationService = Substitute.For<IDeploymentCancellationService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<DeploymentBackgroundJob>>();

        _cancellationService.Register(Arg.Any<Guid>()).Returns(CancellationToken.None);

        _sut = new DeploymentBackgroundJob(
            _projectRepository,
            _orchestrator,
            _cancellationService,
            _unitOfWork,
            _logger);
    }

    [Test]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenProjectNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        // Act
        var result = await _sut.ExecuteAsync(projectId, environmentId, serviceId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenEnvironmentNotFound()
    {
        // Arrange
        var project = Project.Create("test-project");
        var environmentId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var result = await _sut.ExecuteAsync(project.Id, environmentId, serviceId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenServiceNotFound()
    {
        // Arrange
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var serviceId = Guid.NewGuid();

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var result = await _sut.ExecuteAsync(project.Id, environment.Id, serviceId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenDeploymentFails()
    {
        // Arrange
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(
            environment.Id,
            "web",
            ServiceType.DockerImage,
            ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _orchestrator.DeployServiceAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result>(Error.Failed));

        // Act
        var result = await _sut.ExecuteAsync(project.Id, environment.Id, service.Id);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenDeploymentSucceeds()
    {
        // Arrange
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(
            environment.Id,
            "web",
            ServiceType.DockerImage,
            ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _orchestrator.DeployServiceAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        var result = await _sut.ExecuteAsync(project.Id, environment.Id, service.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldCallOrchestrator_WhenAllEntitiesFound()
    {
        // Arrange
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(
            environment.Id,
            "api",
            ServiceType.DockerImage,
            ExposureMode.Internal,
            null, new DockerConfig { Image = "api:latest" });

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _orchestrator.DeployServiceAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        await _sut.ExecuteAsync(project.Id, environment.Id, service.Id);

        // Assert
        await _orchestrator.Received(1).DeployServiceAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldSaveChanges_WhenDeploymentSucceeds()
    {
        // Arrange
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("production");
        var service = project.AddService(
            environment.Id,
            "worker",
            ServiceType.DockerImage,
            ExposureMode.Internal,
            null, new DockerConfig { Image = "worker:1.0" });

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _orchestrator.DeployServiceAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Act
        await _sut.ExecuteAsync(project.Id, environment.Id, service.Id);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldSaveChanges_WhenDeploymentFails()
    {
        // Arrange
        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("staging");
        var service = project.AddService(
            environment.Id,
            "web",
            ServiceType.DockerImage,
            ExposureMode.External,
            null, new DockerConfig { Image = "nginx" });

        _projectRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(project);
        _orchestrator.DeployServiceAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result>(Error.Failed));

        // Act
        await _sut.ExecuteAsync(project.Id, environment.Id, service.Id);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}