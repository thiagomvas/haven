using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class HealthCheckApplyResultTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static HealthCheck NewCheck(int failureThreshold = 1, int successThreshold = 1) =>
        HealthCheck.Create(Guid.NewGuid(), "check", HealthCheckKind.Http, true, null, "{}",
            failureThreshold: failureThreshold, successThreshold: successThreshold);

    private static HealthCheckRunResult Fail(string message = "boom") =>
        HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.Timeout, message, durationMs: 42);

    [Test]
    public void ApplyResult_Unhealthy_DefaultThreshold_FlipsImmediatelyAndStoresDetails()
    {
        var check = NewCheck();

        check.ApplyResult(Fail("timed out"), Now);

        check.LastRunStatus.ShouldBe(ServiceHealth.Unhealthy);
        check.LastRunReason.ShouldBe(HealthCheckFailureReason.Timeout);
        check.LastRunMessage.ShouldBe("timed out");
        check.LastRunDurationMs.ShouldBe(42);
        check.LastRunAt.ShouldBe(Now);
        check.ConsecutiveFailures.ShouldBe(1);
    }

    [Test]
    public void ApplyResult_FailureThreshold_RequiresConsecutiveFailures()
    {
        var check = NewCheck(failureThreshold: 3);

        check.ApplyResult(Fail(), Now);
        check.ApplyResult(Fail(), Now);
        check.LastRunStatus.ShouldBe(ServiceHealth.Unknown);

        check.ApplyResult(Fail(), Now);
        check.LastRunStatus.ShouldBe(ServiceHealth.Unhealthy);
    }

    [Test]
    public void ApplyResult_SuccessInBetween_ResetsFailureCount()
    {
        var check = NewCheck(failureThreshold: 2);

        check.ApplyResult(Fail(), Now);
        check.ApplyResult(HealthCheckRunResult.Healthy(), Now);
        check.ApplyResult(Fail(), Now);

        check.LastRunStatus.ShouldBe(ServiceHealth.Healthy);
        check.ConsecutiveFailures.ShouldBe(1);
    }

    [Test]
    public void ApplyResult_SuccessThreshold_KeepsUnhealthyUntilEnoughSuccesses()
    {
        var check = NewCheck(successThreshold: 2);
        check.ApplyResult(Fail(), Now);

        check.ApplyResult(HealthCheckRunResult.Healthy(), Now);
        check.LastRunStatus.ShouldBe(ServiceHealth.Unhealthy);

        check.ApplyResult(HealthCheckRunResult.Healthy(), Now);
        check.LastRunStatus.ShouldBe(ServiceHealth.Healthy);
    }

    [Test]
    public void ApplyResult_FirstHealthy_IgnoresSuccessThreshold()
    {
        var check = NewCheck(successThreshold: 5);

        check.ApplyResult(HealthCheckRunResult.Healthy(), Now);

        check.LastRunStatus.ShouldBe(ServiceHealth.Healthy);
    }

    [Test]
    public void ApplyResult_Unknown_AppliesImmediatelyAndResetsCounters()
    {
        var check = NewCheck(failureThreshold: 3);
        check.ApplyResult(Fail(), Now);

        check.ApplyResult(HealthCheckRunResult.Unknown(HealthCheckFailureReason.ProbeUnavailable, "no docker"), Now);

        check.LastRunStatus.ShouldBe(ServiceHealth.Unknown);
        check.ConsecutiveFailures.ShouldBe(0);
        check.ConsecutiveSuccesses.ShouldBe(0);
        check.LastRunReason.ShouldBe(HealthCheckFailureReason.ProbeUnavailable);
    }

    [Test]
    public void RunResult_Truncate_LimitsOutputLength()
    {
        var result = HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.Error, "x", output: new string('a', 10_000));

        result.Output!.Length.ShouldBeLessThanOrEqualTo(HealthCheckRunResult.MaxOutputLength + 1);
    }
}
