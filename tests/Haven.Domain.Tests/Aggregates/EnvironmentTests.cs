using Haven.Domain.Enums;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Domain.Tests.Aggregates;

[TestFixture]
[Category("Unit")]
public sealed class EnvironmentTests
{
    private static Environment NewEnvironment() =>
        Environment.Create(Guid.NewGuid(), "test-environment");

    [Test]
    public void GetStatus_NoServices_ReturnsUnknown()
    {
        var environment = NewEnvironment();

        environment.GetStatus().ShouldBe(HealthStatus.Unknown);
    }

    [Test]
    public void GetStatus_AllServicesRunning_ReturnsHealthy()
    {
        var environment = NewEnvironment();
        var service = environment.AddService("svc", ServiceType.DockerImage, ExposureMode.None);
        service.MarkDeployed();

        environment.GetStatus().ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public void GetStatus_SomeServicesRunningSomeNot_ReturnsDegraded()
    {
        var environment = NewEnvironment();
        var running = environment.AddService("svc-running", ServiceType.DockerImage, ExposureMode.None);
        running.MarkDeployed();
        environment.AddService("svc-stopped", ServiceType.DockerImage, ExposureMode.None);

        environment.GetStatus().ShouldBe(HealthStatus.Degraded);
    }

    [Test]
    public void GetStatus_NoneRunningButOneDeploying_ReturnsDeploying()
    {
        var environment = NewEnvironment();
        var service = environment.AddService("svc", ServiceType.DockerImage, ExposureMode.None);
        service.MarkDeploying();

        environment.GetStatus().ShouldBe(HealthStatus.Deploying);
    }

    [Test]
    public void GetStatus_NoneRunningButOneDeploymentPending_ReturnsDeploymentPending()
    {
        var environment = NewEnvironment();
        var service = environment.AddService("svc", ServiceType.DockerImage, ExposureMode.None);
        service.MarkDeploymentPending();

        environment.GetStatus().ShouldBe(HealthStatus.DeploymentPending);
    }

    [Test]
    public void GetStatus_AllServicesStopped_ReturnsStopped()
    {
        var environment = NewEnvironment();
        environment.AddService("svc", ServiceType.DockerImage, ExposureMode.None);

        environment.GetStatus().ShouldBe(HealthStatus.Stopped);
    }
}