using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Events;

using Shouldly;

namespace Haven.Domain.Tests.Aggregates;

[TestFixture]
[Category("Unit")]
public sealed class ServiceHealthTests
{
    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public void RecordHealthCheckResult_Unhealthy_SetsServiceHealthAndRaisesServiceDegradedEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Unhealthy);

        service.Health.ShouldBe(ServiceHealth.Unhealthy);
        healthCheck.LastRunStatus.ShouldBe(ServiceHealth.Unhealthy);
        service.DomainEvents.ShouldContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_StaysUnhealthy_DoesNotRaiseDuplicateServiceDegradedEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Unhealthy);
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Unhealthy);

        service.Health.ShouldBe(ServiceHealth.Unhealthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_Healthy_DoesNotRaiseServiceDegradedEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Healthy);

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_RecoversFromUnhealthy_RaisesServiceRecoveredEventNotDegraded()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Unhealthy);
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Healthy);

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldContain(e => e is ServiceRecoveredEvent);
        service.DomainEvents.ShouldNotContain(e => e is ServiceDegradedEvent);
    }

    [Test]
    public void RecordHealthCheckResult_StaysHealthy_DoesNotRaiseServiceRecoveredEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Healthy);
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Healthy);

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceRecoveredEvent);
    }

    [Test]
    public void RecordHealthCheckResult_RecoversFromUnknown_DoesNotRaiseServiceRecoveredEvent()
    {
        var service = NewService();
        var healthCheck = service.AddHealthCheck("http-check", HealthCheckKind.Http, enabled: true, cronExpression: null, config: "{}");
        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Unknown);
        service.ClearDomainEvents();

        service.RecordHealthCheckResult(healthCheck, ServiceHealth.Healthy);

        service.Health.ShouldBe(ServiceHealth.Healthy);
        service.DomainEvents.ShouldNotContain(e => e is ServiceRecoveredEvent);
    }
}