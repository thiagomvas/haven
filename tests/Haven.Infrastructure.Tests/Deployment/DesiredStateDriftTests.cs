using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[TestFixture]
[Category("Unit")]
public sealed class DesiredStateDriftTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Settled = Now - DesiredStateDrift.SettleTime - TimeSpan.FromSeconds(1);

    [TestCase(DesiredState.Running, ServiceRuntimeState.Stopped, DriftAction.Start)]
    [TestCase(DesiredState.Running, ServiceRuntimeState.Crashed, DriftAction.Start)]
    [TestCase(DesiredState.Running, ServiceRuntimeState.Running, DriftAction.None)]
    [TestCase(DesiredState.Running, ServiceRuntimeState.NotDeployed, DriftAction.None)]
    [TestCase(DesiredState.Running, ServiceRuntimeState.Restarting, DriftAction.None)]
    [TestCase(DesiredState.Running, ServiceRuntimeState.Unknown, DriftAction.None)]
    [TestCase(DesiredState.Stopped, ServiceRuntimeState.Running, DriftAction.Stop)]
    [TestCase(DesiredState.Stopped, ServiceRuntimeState.Stopped, DriftAction.None)]
    [TestCase(DesiredState.Stopped, ServiceRuntimeState.Crashed, DriftAction.Stop)]
    [TestCase(DesiredState.Stopped, ServiceRuntimeState.NotDeployed, DriftAction.None)]
    [TestCase(DesiredState.Stopped, ServiceRuntimeState.Restarting, DriftAction.Stop)]
    [TestCase(DesiredState.Stopped, ServiceRuntimeState.Unknown, DriftAction.None)]
    public void Decide_SettledState_ConvergesTowardDesired(DesiredState desired, ServiceRuntimeState runtime, DriftAction expected)
    {
        DesiredStateDrift.Decide(desired, runtime, deploymentActive: false, Settled, Now).ShouldBe(expected);
    }

    [Test]
    public void Decide_WhileDeploymentIsActive_DoesNothing()
    {
        DesiredStateDrift.Decide(DesiredState.Running, ServiceRuntimeState.Stopped, deploymentActive: true, Settled, Now)
            .ShouldBe(DriftAction.None);
    }

    [Test]
    public void Decide_RecentlyChangedState_DoesNothingYet()
    {
        DesiredStateDrift.Decide(DesiredState.Running, ServiceRuntimeState.Crashed, deploymentActive: false, Now.AddSeconds(-5), Now)
            .ShouldBe(DriftAction.None);
    }

    [TestCase(RestartPolicy.No, ServiceRuntimeState.Stopped, DriftAction.None)]
    [TestCase(RestartPolicy.No, ServiceRuntimeState.Crashed, DriftAction.None)]
    [TestCase(RestartPolicy.OnFailure, ServiceRuntimeState.Stopped, DriftAction.None)]
    [TestCase(RestartPolicy.OnFailure, ServiceRuntimeState.Crashed, DriftAction.Start)]
    [TestCase(RestartPolicy.Always, ServiceRuntimeState.Stopped, DriftAction.Start)]
    [TestCase(RestartPolicy.UnlessStopped, ServiceRuntimeState.Crashed, DriftAction.Start)]
    public void Decide_DesiredRunning_RespectsRestartPolicy(RestartPolicy policy, ServiceRuntimeState runtime, DriftAction expected)
    {
        DesiredStateDrift.Decide(DesiredState.Running, runtime, deploymentActive: false, Settled, Now, policy).ShouldBe(expected);
    }
}