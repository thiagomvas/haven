using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class HealthCheckJobTests
{
    private IHealthCheckRepository _healthCheckRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IHealthCheckRunnerFactory _runnerFactory = null!;
    private IUnitOfWork _unitOfWork = null!;
    private HealthCheckJob _sut = null!;

    [SetUp]
    public void Setup()
    {
        _healthCheckRepository = Substitute.For<IHealthCheckRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _runnerFactory = Substitute.For<IHealthCheckRunnerFactory>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _sut = new HealthCheckJob(
            _healthCheckRepository,
            _serviceRepository,
            _runnerFactory,
            _unitOfWork,
            Substitute.For<ILogger<HealthCheckJob>>());
    }

    [Test]
    public async Task ExecuteAsync_WhenHealthCheckNotFound_ShouldDoNothing()
    {
        var healthCheckId = Guid.NewGuid();
        _healthCheckRepository.GetByIdAsync(healthCheckId, Arg.Any<CancellationToken>())
            .Returns((HealthCheck?)null);

        await _sut.ExecuteAsync(healthCheckId, CancellationToken.None);

        await _serviceRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        _runnerFactory.DidNotReceiveWithAnyArgs().Create(default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenServiceNotFound_ShouldDoNothing()
    {
        var serviceId = Guid.NewGuid();
        var healthCheck = HealthCheck.Create(serviceId, "check", HealthCheckKind.Http, true, null, "{}");

        _healthCheckRepository.GetByIdAsync(healthCheck.Id, Arg.Any<CancellationToken>())
            .Returns(healthCheck);
        _serviceRepository.GetByIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((Service?)null);

        await _sut.ExecuteAsync(healthCheck.Id, CancellationToken.None);

        _runnerFactory.DidNotReceiveWithAnyArgs().Create(default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenHealthCheckAndServiceExist_ShouldRunCheckAndRecordResultAndSaveChanges()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);
        var healthCheck = service.AddHealthCheck("check", HealthCheckKind.Http, true, null, "{}");
        var runner = Substitute.For<IHealthCheckRunner>();

        _healthCheckRepository.GetByIdAsync(healthCheck.Id, Arg.Any<CancellationToken>())
            .Returns(healthCheck);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _runnerFactory.Create(HealthCheckKind.Http).Returns(runner);
        runner.RunHealthCheckAsync(healthCheck, Arg.Any<CancellationToken>())
            .Returns(ServiceHealth.Healthy);

        await _sut.ExecuteAsync(healthCheck.Id, CancellationToken.None);

        healthCheck.LastRunStatus.ShouldBe(ServiceHealth.Healthy);
        healthCheck.LastRunAt.ShouldNotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_ShouldUseRunnerForHealthCheckKind()
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);
        var healthCheck = service.AddHealthCheck("check", HealthCheckKind.Bash, true, null, "{}");
        var runner = Substitute.For<IHealthCheckRunner>();

        _healthCheckRepository.GetByIdAsync(healthCheck.Id, Arg.Any<CancellationToken>())
            .Returns(healthCheck);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns(service);
        _runnerFactory.Create(HealthCheckKind.Bash).Returns(runner);
        runner.RunHealthCheckAsync(healthCheck, Arg.Any<CancellationToken>())
            .Returns(ServiceHealth.Unhealthy);

        await _sut.ExecuteAsync(healthCheck.Id, CancellationToken.None);

        _runnerFactory.Received(1).Create(HealthCheckKind.Bash);
        await runner.Received(1).RunHealthCheckAsync(healthCheck, Arg.Any<CancellationToken>());
    }
}