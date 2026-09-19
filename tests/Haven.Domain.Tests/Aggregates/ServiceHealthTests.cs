using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Events;
using Haven.Domain.Models;

using Shouldly;

namespace Haven.Domain.Tests.Aggregates;

[TestFixture]
[Category("Unit")]
public sealed class ServiceHealthTests
{
    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    private static HealthCheckRunResult Failed() =>
        HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.ConnectionRefused, "connection refused");

    [Test]
    public void RecordHealthCheckResult_Unhealthy_SetsServiceHealthAndRaisesServiceDegradedEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, Failed());

        service.Health.ShouldBe(ServiceHealth.Unhealthy);
        healthCheck.LastRunStatus.ShouldBe(ServiceHealth.Unhealthy);
        service.DomainEvents.ShouldContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_StaysUnhealthy_DoesNotRaiseDuplicateServiceDegradedEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, Failed());
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, Failed());

        service.Health.ShouldBe(ServiceHealth.Unhealthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_Healthy_DoesNotRaiseServiceDegradedEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, HealthCheckRunResult.Healthy());

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_RecoversFromUnhealthy_RaisesServiceRecoveredEventNotDegraded()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, Failed());
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, HealthCheckRunResult.Healthy());

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldContain(e => e is ServiceRecoveredEvent);
        service.DomainEvents.ShouldNotContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_StaysHealthy_DoesNotRaiseServiceRecoveredEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, HealthCheckRunResult.Healthy());
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, HealthCheckRunResult.Healthy());

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceRecoveredEvent);
    }

    [Test]
    public void RecordHealthCheckResult_RecoversFromUnknown_DoesNotRaiseServiceRecoveredEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, HealthCheckRunResult.Unknown(HealthCheckFailureReason.ProbeUnavailable, "probe"));
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, HealthCheckRunResult.Healthy());

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceRecoveredEvent);
    }

    [Test]
    public void RecordHealthCheckResult_Unhealthy_DegradedEventCarriesCheckNameAndReason()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, Failed());

        var degraded = service.DomainEvents.OfType<ServiceDegradedEvent>().ShouldHaveSingleItem();
        degraded.HealthCheckName.ShouldBe("http-check");
        degraded.Reason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
        degraded.ToMessage().ShouldContain("connection refused");
    }

    [Test]
    public void RecordHealthCheckResult_BelowFailureThreshold_DoesNotDegradeService()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, true, null, "{}", failureThreshold: 3);
        service.RecordHealthCheckResult(healthCheck, HealthCheckRunResult.Healthy());
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, Failed());
        service.RecordHealthCheckResult(healthCheck, Failed());

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceDegradedEvent);

        service.RecordHealthCheckResult(healthCheck, Failed());

        service.Health.ShouldBe(ServiceHealth.Unhealthy);
        service.DomainEvents.ShouldContain(e => e is ServiceDegradedEvent);
    }
}
