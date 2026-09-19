using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.HealthChecks.Commands.CreateHealthCheckCommand;
using Haven.Application.Features.HealthChecks.Commands.RunHealthCheckNowCommand;
using Haven.Application.Features.HealthChecks.Commands.TestHealthCheckCommand;
using Haven.Application.Features.HealthChecks.Commands.UpdateHealthCheckCommand;
using Haven.Application.Features.HealthChecks.Queries.GetHealthCheckResultsQuery;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.HealthChecks;

[Category("Unit")]
public sealed class HealthCheckHandlerTests
{
    private IHealthCheckRepository _healthChecks = null!;
    private IServiceRepository _services = null!;
    private IHealthCheckExecutor _executor = null!;
    private IHealthCheckScheduler _scheduler = null!;

    [SetUp]
    public void Setup()
    {
        _healthChecks = Substitute.For<IHealthCheckRepository>();
        _services = Substitute.For<IServiceRepository>();
        _executor = Substitute.For<IHealthCheckExecutor>();
        _scheduler = Substitute.For<IHealthCheckScheduler>();
    }

    private static Service NewService() => Service.Create(Guid.NewGuid(), "svc", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public async Task RunNow_ReturnsTheExecutorResultSynchronously()
    {
        var service = NewService();
        var check = service.AddHealthCheck("c", HealthCheckKind.Http, true, null, "{}");
        _healthChecks.GetByIdAsync(check.Id, Arg.Any<CancellationToken>()).Returns(check);
        _executor.ExecuteAsync(check.Id, Arg.Any<CancellationToken>())
            .Returns(HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.Timeout, "timed out", 5000));

        var result = await new RunHealthCheckNowHandler(_healthChecks, _executor)
            .Handle(new RunHealthCheckNowCommand { HealthCheckId = check.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Value.Reason.ShouldBe(HealthCheckFailureReason.Timeout);
        result.Value.Message.ShouldBe("timed out");
    }

    [Test]
    public async Task RunNow_UnknownHealthCheck_ReturnsNotFound()
    {
        _healthChecks.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((HealthCheck?)null);

        var result = await new RunHealthCheckNowHandler(_healthChecks, _executor)
            .Handle(new RunHealthCheckNowCommand { HealthCheckId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _executor.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default);
    }

    [Test]
    public async Task Test_RunsTransientCheckAgainstServiceWithoutPersisting()
    {
        var service = NewService();
        _services.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _executor.TestAsync(Arg.Any<HealthCheck>(), Arg.Any<CancellationToken>())
            .Returns(HealthCheckRunResult.Healthy("HTTP 200", 20, 200));

        var result = await new TestHealthCheckHandler(_services, _executor).Handle(
            new TestHealthCheckCommand { ServiceId = service.Id, Kind = HealthCheckKind.Http, Config = """{"url":"http://x"}""" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.HttpStatusCode.ShouldBe(200);
        await _executor.Received(1).TestAsync(
            Arg.Is<HealthCheck>(h => h.ServiceId == service.Id && h.Kind == HealthCheckKind.Http && h.Config == """{"url":"http://x"}"""),
            Arg.Any<CancellationToken>());
        service.HealthChecks.ShouldBeEmpty();
    }

    [Test]
    public async Task Test_UnknownService_ReturnsNotFound()
    {
        _services.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await new TestHealthCheckHandler(_services, _executor).Handle(
            new TestHealthCheckCommand { ServiceId = Guid.NewGuid(), Kind = HealthCheckKind.Container }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Results_ReturnsHistoryNewestFirstAsProvidedByRepository()
    {
        var check = HealthCheck.Create(Guid.NewGuid(), "c", HealthCheckKind.Http, true, null, "{}");
        _healthChecks.GetByIdAsync(check.Id, Arg.Any<CancellationToken>()).Returns(check);
        _healthChecks.GetResultsAsync(check.Id, 10, Arg.Any<CancellationToken>()).Returns(new List<HealthCheckResult>
        {
            HealthCheckResult.Create(check.Id, HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.ConnectionRefused, "refused", 5), DateTime.UtcNow),
            HealthCheckResult.Create(check.Id, HealthCheckRunResult.Healthy("ok", 7, 200), DateTime.UtcNow.AddMinutes(-1))
        });

        var result = await new GetHealthCheckResultsHandler(_healthChecks)
            .Handle(new GetHealthCheckResultsQuery { HealthCheckId = check.Id, Limit = 10 }, CancellationToken.None);

        result.Value.Count.ShouldBe(2);
        result.Value[0].Reason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
        result.Value[1].HttpStatusCode.ShouldBe(200);
    }

    [Test]
    public async Task Create_PassesRetriesAndThresholdsToTheCheck()
    {
        var service = NewService();
        _services.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await new CreateHealthCheckHandler(_services, _healthChecks, _scheduler).Handle(new CreateHealthCheckCommand
        {
            ServiceId = service.Id,
            Name = "api",
            Kind = HealthCheckKind.Tcp,
            Config = """{"host":"h","port":1}""",
            Retries = 2,
            FailureThreshold = 3,
            SuccessThreshold = 4
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var check = service.HealthChecks.ShouldHaveSingleItem();
        check.Retries.ShouldBe(2);
        check.FailureThreshold.ShouldBe(3);
        check.SuccessThreshold.ShouldBe(4);
    }

    [Test]
    public async Task Update_OnlyChangesProvidedSettings()
    {
        var service = NewService();
        var check = service.AddHealthCheck("api", HealthCheckKind.Tcp, true, null, "{}", retries: 1, failureThreshold: 2, successThreshold: 2);
        _healthChecks.GetByIdAsync(check.Id, Arg.Any<CancellationToken>()).Returns(check);
        _services.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await new UpdateHealthCheckHandler(_healthChecks, _services, _scheduler)
            .Handle(new UpdateHealthCheckCommand { HealthCheckId = check.Id, FailureThreshold = 5 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        check.FailureThreshold.ShouldBe(5);
        check.Retries.ShouldBe(1);
        check.SuccessThreshold.ShouldBe(2);
    }
}
