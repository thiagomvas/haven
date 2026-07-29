using System.Net;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Telemetry;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class DeploymentOrchestratorTests
{
    private DeploymentOrchestrator _sut = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IServiceRegistry _registry = null!;
    private IDeployServiceFactory _deployServiceFactory = null!;
    private IDeploymentLogService _logService = null!;
    private IDeployService _deployService = null!;

    [SetUp]
    public void Setup()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _registry = Substitute.For<IServiceRegistry>();
        _deployServiceFactory = Substitute.For<IDeployServiceFactory>();
        _logService = Substitute.For<IDeploymentLogService>();
        _deployService = Substitute.For<IDeployService>();

        _sut = new DeploymentOrchestrator(_unitOfWork, _registry, _deployServiceFactory, _logService, new HavenMetrics(), Substitute.For<ILogger<DeploymentOrchestrator>>());

        _logService.CreateDeploymentForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Haven.Domain.Entities.Deployment.Create(Guid.NewGuid(), "log.txt"));

        _deployServiceFactory.Create(Arg.Any<Service>()).Returns(_deployService);

        _registry.EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x => ServiceRegistryEntry.Create(x.ArgAt<Guid>(0)));
    }

    [Test]
    public async Task DeployServiceAsync_WhenServiceIsNull_ShouldReturnNotFound()
    {
        var result = await _sut.DeployServiceAsync(null!, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound);
    }

    [Test]
    public async Task DeployServiceAsync_WhenServiceHasNoEnvironmentProject_ShouldReturnNotFound()
    {
        var service = CreateService(withEnvironment: false);

        var result = await _sut.DeployServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound);
    }

    [Test]
    public async Task DeployServiceAsync_WhenNoDeployServiceAvailable_ShouldReturnFailure()
    {
        var service = CreateService();
        _deployServiceFactory.Create(service).Returns((IDeployService?)null);

        var result = await _sut.DeployServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployFails_ShouldReturnFailure()
    {
        var service = CreateService();
        var deployError = Error.Failed;
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(deployError));

        var result = await _sut.DeployServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(deployError);
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployFails_ShouldMarkDeploymentFailed()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(Error.Failed));

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _logService.Received(1).MarkDeploymentFailedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployFails_ShouldMarkServiceStopped()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(Error.Failed));

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployFails_ShouldNotTouchRegistry()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(Error.Failed));

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _registry.DidNotReceive().EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployServiceAsync_WhenCancelled_ShouldReturnCancelledFailure()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<OperationCanceledException>();

        var result = await _sut.DeployServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task DeployServiceAsync_WhenCancelled_ShouldMarkDeploymentCancelled()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<OperationCanceledException>();

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _logService.Received(1).MarkDeploymentCancelledAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployThrowsUnexpectedException_ShouldReturnFailure()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<InvalidOperationException>();

        var result = await _sut.DeployServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployThrowsUnexpectedException_ShouldMarkDeploymentFailed()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<InvalidOperationException>();

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _logService.Received(1).MarkDeploymentFailedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployThrowsUnexpectedException_ShouldMarkServiceStopped()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<InvalidOperationException>();

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task DeployServiceAsync_WhenDeployThrowsUnexpectedException_ShouldNotTouchRegistry()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<InvalidOperationException>();

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _registry.DidNotReceive().EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployServiceAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));

        var result = await _sut.DeployServiceAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task DeployServiceAsync_WhenSuccessful_ShouldEnsureServiceRegistered()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _registry.EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(ServiceRegistryEntry.Create(service.Id));

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _registry.Received(1).EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployServiceAsync_WhenSuccessful_ShouldUpdateRegistryRuntimeData()
    {
        var service = CreateService();
        var ip = IPAddress.Parse("172.17.0.2");
        var ports = new List<PortMapping> { new(8080, 80) };
        var deployData = new DeployData { ServiceId = service.Id, IpAddress = ip, Ports = ports, ContainerName = "my-container" };
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(deployData));
        var entry = ServiceRegistryEntry.Create(service.Id);
        _registry.EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        entry.IpAddress.ShouldBe("172.17.0.2");
        entry.Ports.ShouldBe(ports);
        entry.ContainerName.ShouldBe("my-container");
    }

    [Test]
    public async Task DeployServiceAsync_WhenSuccessful_ShouldMarkDeploymentCompleted()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _registry.EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceRegistryEntry.Create(service.Id));

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _logService.Received(1).MarkDeploymentCompletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployServiceAsync_WhenContainerDiedWhileDeploying_ShouldNotOverwriteStoppedStatus()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        // Simulates a reactive Docker "die" event handler marking the service Stopped,
        // via a different unit of work, while DeployAsync was still in flight.
        _unitOfWork.ReloadAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Service>().MarkStopped();
                return Task.CompletedTask;
            });

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task DeployServiceAsync_WhenContainerDiedWhileDeploying_ShouldReturnFailure()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _unitOfWork.ReloadAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Service>().MarkStopped();
                return Task.CompletedTask;
            });

        var result = await _sut.DeployServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Docker.ContainerCrashedAfterStart);
    }

    [Test]
    public async Task DeployServiceAsync_WhenContainerDiedWhileDeploying_ShouldNotTouchRegistry()
    {
        var service = CreateService();
        _deployService.DeployAsync(service, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _unitOfWork.ReloadAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Service>().MarkStopped();
                return Task.CompletedTask;
            });

        await _sut.DeployServiceAsync(service, CancellationToken.None);

        await _registry.DidNotReceive().EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopServiceAsync_WhenNoDeployServiceAvailable_ShouldReturnFailure()
    {
        var service = CreateService();
        _deployServiceFactory.Create(service).Returns((IDeployService?)null);

        var result = await _sut.StopServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task StopServiceAsync_WhenStopFails_ShouldReturnFailure()
    {
        var service = CreateService();
        var stopError = Error.Failed;
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(stopError));

        var result = await _sut.StopServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(stopError);
    }

    [Test]
    public async Task StopServiceAsync_WhenSuccessful_ShouldMarkServiceStopped()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        await _sut.StopServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task StopServiceAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.StopServiceAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task StopServiceAsync_WhenContainerKillEventAlreadyMarkedStopped_ShouldNotRaiseDuplicateStoppedEvent()
    {
        var service = CreateService();
        service.MarkDeployed();
        service.ClearDomainEvents();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        // Simulates the Docker daemon's kill/die event being processed (via its own
        // DbContext scope) and marking the service Stopped while StopAsync was in flight.
        _unitOfWork.ReloadAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Service>().MarkStopped();
                return Task.CompletedTask;
            });

        await _sut.StopServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
        service.DomainEvents.OfType<ServiceStoppedEvent>().Count().ShouldBe(1);
    }

    [Test]
    public async Task StartServiceAsync_WhenNoDeployServiceAvailable_ShouldReturnFailure()
    {
        var service = CreateService();
        _deployServiceFactory.Create(service).Returns((IDeployService?)null);

        var result = await _sut.StartServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task StartServiceAsync_WhenStartFails_ShouldReturnFailure()
    {
        var service = CreateService();
        var startError = Error.Failed;
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(startError));

        var result = await _sut.StartServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(startError);
    }

    [Test]
    public async Task StartServiceAsync_WhenStartFails_ShouldMarkServiceStopped()
    {
        var service = CreateService();
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(Error.Failed));

        await _sut.StartServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task StartServiceAsync_WhenStartFails_ShouldNotTouchRegistry()
    {
        var service = CreateService();
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(Error.Failed));

        await _sut.StartServiceAsync(service, CancellationToken.None);

        await _registry.DidNotReceive().EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartServiceAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        var service = CreateService();
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _registry.EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceRegistryEntry.Create(service.Id));

        var result = await _sut.StartServiceAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task StartServiceAsync_WhenSuccessful_ShouldEnsureServiceRegistered()
    {
        var service = CreateService();
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _registry.EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(ServiceRegistryEntry.Create(service.Id));

        await _sut.StartServiceAsync(service, CancellationToken.None);

        await _registry.Received(1).EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartServiceAsync_WhenSuccessful_ShouldUpdateRegistryRuntimeData()
    {
        var service = CreateService();
        var ip = IPAddress.Parse("172.17.0.3");
        var ports = new List<PortMapping> { new(9090, 90) };
        var startData = new DeployData { ServiceId = service.Id, IpAddress = ip, Ports = ports, ContainerName = "started-container" };
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(startData));
        var entry = ServiceRegistryEntry.Create(service.Id);
        _registry.EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        await _sut.StartServiceAsync(service, CancellationToken.None);

        entry.IpAddress.ShouldBe("172.17.0.3");
        entry.Ports.ShouldBe(ports);
        entry.ContainerName.ShouldBe("started-container");
    }

    [Test]
    public async Task StartServiceAsync_WhenContainerDiedWhileStarting_ShouldNotOverwriteStoppedStatus()
    {
        var service = CreateService();
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _unitOfWork.ReloadAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<Service>().MarkStopped();
                return Task.CompletedTask;
            });

        var result = await _sut.StartServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Docker.ContainerCrashedAfterStart);
    }

    [Test]
    public async Task RestartServiceAsync_WhenNoDeployServiceAvailable_ShouldReturnFailure()
    {
        var service = CreateService();
        _deployServiceFactory.Create(service).Returns((IDeployService?)null);

        var result = await _sut.RestartServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task RestartServiceAsync_WhenStopFails_ShouldReturnFailure()
    {
        var service = CreateService();
        var stopError = Error.Failed;
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(stopError));

        var result = await _sut.RestartServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(stopError);
    }

    [Test]
    public async Task RestartServiceAsync_WhenStopFails_ShouldMarkServiceStopped()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failed));

        await _sut.RestartServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task RestartServiceAsync_WhenStartFails_ShouldReturnFailure()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var startError = Error.Failed;
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(startError));

        var result = await _sut.RestartServiceAsync(service, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(startError);
    }

    [Test]
    public async Task RestartServiceAsync_WhenStartFails_ShouldMarkServiceStopped()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(Error.Failed));

        await _sut.RestartServiceAsync(service, CancellationToken.None);

        service.Status.ShouldBe(ServiceStatus.Stopped);
    }

    [Test]
    public async Task RestartServiceAsync_WhenStartFails_ShouldNotTouchRegistry()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Failure(Error.Failed));

        await _sut.RestartServiceAsync(service, CancellationToken.None);

        await _registry.DidNotReceive().EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestartServiceAsync_WhenSuccessful_ShouldReturnSuccess()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _registry.EnsureServiceRegisteredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceRegistryEntry.Create(service.Id));

        var result = await _sut.RestartServiceAsync(service, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task RestartServiceAsync_WhenSuccessful_ShouldEnsureServiceRegistered()
    {
        var service = CreateService();
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(new DeployData { ServiceId = service.Id }));
        _registry.EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(ServiceRegistryEntry.Create(service.Id));

        await _sut.RestartServiceAsync(service, CancellationToken.None);

        await _registry.Received(1).EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RestartServiceAsync_WhenSuccessful_ShouldUpdateRegistryRuntimeData()
    {
        var service = CreateService();
        var ip = IPAddress.Parse("10.0.0.1");
        var ports = new List<PortMapping> { new(3000, 3000) };
        var startData = new DeployData { ServiceId = service.Id, IpAddress = ip, Ports = ports, ContainerName = "restarted-container" };
        _deployService.StopAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _deployService.StartAsync(service, Arg.Any<CancellationToken>())
            .Returns(Result<DeployData>.Success(startData));
        var entry = ServiceRegistryEntry.Create(service.Id);
        _registry.EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(entry);

        await _sut.RestartServiceAsync(service, CancellationToken.None);

        entry.IpAddress.ShouldBe("10.0.0.1");
        entry.Ports.ShouldBe(ports);
        entry.ContainerName.ShouldBe("restarted-container");
    }

    private static Service CreateService(bool withEnvironment = true)
    {
        if (!withEnvironment)
            return Service.Create(Guid.NewGuid(), "test-svc", ServiceType.DockerImage, ExposureMode.Internal,
                sourceConfig: new DockerConfig { Image = "myapp:latest" });

        var project = Project.Create("test-project");
        var environment = project.AddEnvironment("dev");
        var service = project.AddService(environment.Id, "test-svc", ServiceType.DockerImage, ExposureMode.Internal,
            null, new DockerConfig { Image = "myapp:latest" });
        service.Environment = environment;
        service.Environment.Project = project;
        return service;
    }
}