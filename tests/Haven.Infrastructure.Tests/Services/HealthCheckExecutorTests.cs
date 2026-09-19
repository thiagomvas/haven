using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Events;
using Haven.Domain.Models;
using Haven.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class HealthCheckExecutorTests
{
    private IHealthCheckRepository _healthCheckRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private IHealthCheckRunnerFactory _runnerFactory = null!;
    private IHealthCheckRunner _runner = null!;
    private IUnitOfWork _unitOfWork = null!;
    private HealthCheckOptions _options = null!;
    private HealthCheckExecutor _sut = null!;

    [SetUp]
    public void Setup()
    {
        _healthCheckRepository = Substitute.For<IHealthCheckRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _runnerFactory = Substitute.For<IHealthCheckRunnerFactory>();
        _runner = Substitute.For<IHealthCheckRunner>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _options = new HealthCheckOptions { RetryDelaySeconds = 0, ResultRetentionCount = 25 };

        var optionsMonitor = Substitute.For<IOptionsMonitor<HealthCheckOptions>>();
        optionsMonitor.CurrentValue.Returns(_ => _options);

        _runnerFactory.Create(Arg.Any<HealthCheckKind>()).Returns(_runner);

        _sut = new HealthCheckExecutor(
            _healthCheckRepository,
            _serviceRepository,
            _runnerFactory,
            _unitOfWork,
            optionsMonitor,
            Substitute.For<ILogger<HealthCheckExecutor>>());
    }

    private (Service Service, HealthCheck Check) Arrange(int retries = 0, int failureThreshold = 1)
    {
        var service = Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);
        var check = service.AddHealthCheck("check", HealthCheckKind.Http, true, null, "{}", retries, failureThreshold);

        _healthCheckRepository.GetByIdAsync(check.Id, Arg.Any<CancellationToken>()).Returns(check);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        return (service, check);
    }

    private static HealthCheckRunResult Failed(string message = "boom") =>
        HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.ConnectionRefused, message);

    [Test]
    public async Task ExecuteAsync_WhenHealthCheckNotFound_ReturnsNullAndDoesNothing()
    {
        var id = Guid.NewGuid();
        _healthCheckRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((HealthCheck?)null);

        var result = await _sut.ExecuteAsync(id);

        result.ShouldBeNull();
        _runnerFactory.DidNotReceiveWithAnyArgs().Create(default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenServiceNotFound_ReturnsNullAndDoesNothing()
    {
        var check = HealthCheck.Create(Guid.NewGuid(), "check", HealthCheckKind.Http, true, null, "{}");
        _healthCheckRepository.GetByIdAsync(check.Id, Arg.Any<CancellationToken>()).Returns(check);
        _serviceRepository.GetByIdAsync(check.ServiceId, Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.ExecuteAsync(check.Id);

        result.ShouldBeNull();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_Success_RecordsResultPersistsHistoryAndTrims()
    {
        var (_, check) = Arrange();
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>()).Returns(HealthCheckRunResult.Healthy("ok", 12, 200));

        var result = await _sut.ExecuteAsync(check.Id);

        result!.Status.ShouldBe(ServiceHealth.Healthy);
        check.LastRunStatus.ShouldBe(ServiceHealth.Healthy);
        check.LastRunAt.ShouldNotBeNull();
        await _healthCheckRepository.Received(1).AddResultAsync(
            Arg.Is<HealthCheckResult>(r => r.HealthCheckId == check.Id && r.Status == ServiceHealth.Healthy && r.HttpStatusCode == 200),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _healthCheckRepository.Received(1).TrimResultsAsync(check.Id, 25, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_Failure_StoresReasonAndRaisesDegradedEventWithDetails()
    {
        var (service, check) = Arrange();
        service.ClearDomainEvents();
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>()).Returns(Failed("connection refused"));

        await _sut.ExecuteAsync(check.Id);

        check.LastRunReason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
        check.LastRunMessage.ShouldBe("connection refused");
        var degraded = service.DomainEvents.OfType<ServiceDegradedEvent>().ShouldHaveSingleItem();
        degraded.HealthCheckName.ShouldBe("check");
        degraded.Message.ShouldBe("connection refused");
    }

    [Test]
    public async Task ExecuteAsync_LoadsAllServiceChecksBeforeRollUp()
    {
        var (service, check) = Arrange();
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>()).Returns(HealthCheckRunResult.Healthy());

        await _sut.ExecuteAsync(check.Id);

        await _healthCheckRepository.Received(1).GetForServiceListAsync(service.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WithRetries_RetriesUntilHealthy()
    {
        var (_, check) = Arrange(retries: 2);
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>())
            .Returns(Failed(), Failed(), HealthCheckRunResult.Healthy());

        var result = await _sut.ExecuteAsync(check.Id);

        result!.Status.ShouldBe(ServiceHealth.Healthy);
        result.Attempts.ShouldBe(3);
        await _runner.Received(3).RunHealthCheckAsync(check, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WithRetries_StopsAfterMaxAttemptsAndReportsFailure()
    {
        var (_, check) = Arrange(retries: 1);
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>()).Returns(Failed());

        var result = await _sut.ExecuteAsync(check.Id);

        result!.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Attempts.ShouldBe(2);
        await _runner.Received(2).RunHealthCheckAsync(check, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_UnknownResult_IsNotRetried()
    {
        var (_, check) = Arrange(retries: 3);
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>())
            .Returns(HealthCheckRunResult.Unknown(HealthCheckFailureReason.ProbeUnavailable, "no docker"));

        await _sut.ExecuteAsync(check.Id);

        await _runner.Received(1).RunHealthCheckAsync(check, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenRunnerThrows_RecordsUnknownErrorInsteadOfFailing()
    {
        var (_, check) = Arrange();
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>())
            .Returns<HealthCheckRunResult>(_ => throw new InvalidOperationException("docker exploded"));

        var result = await _sut.ExecuteAsync(check.Id);

        result!.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.Error);
        result.Message.ShouldContain("docker exploded");
        check.LastRunStatus.ShouldBe(ServiceHealth.Unknown);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenTrimFails_StillReturnsResult()
    {
        var (_, check) = Arrange();
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>()).Returns(HealthCheckRunResult.Healthy());
        _healthCheckRepository.TrimResultsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("db down"));

        var result = await _sut.ExecuteAsync(check.Id);

        result!.Status.ShouldBe(ServiceHealth.Healthy);
    }

    [Test]
    public async Task TestAsync_RunsOnceWithoutPersistingAnything()
    {
        var check = HealthCheck.Create(Guid.NewGuid(), "test", HealthCheckKind.Http, true, null, "{}", retries: 3);
        _runner.RunHealthCheckAsync(check, Arg.Any<CancellationToken>()).Returns(Failed());

        var result = await _sut.TestAsync(check);

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        await _runner.Received(1).RunHealthCheckAsync(check, Arg.Any<CancellationToken>());
        await _healthCheckRepository.DidNotReceiveWithAnyArgs().AddResultAsync(default!, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
