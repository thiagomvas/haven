using Haven.Domain.Enums;

namespace Haven.Infrastructure.Deployment;

public enum DriftAction
{
    None,
    Start,
    Stop
}

/// <summary>Decides what, if anything, converges a container's observed runtime state toward its desired state.</summary>
public static class DesiredStateDrift
{
    /// <summary>How long a runtime state must have been stable before it is treated as drift, so the reconciler never races a transition that is still settling.</summary>
    public static readonly TimeSpan SettleTime = TimeSpan.FromMinutes(2);

    public static DriftAction Decide(
        DesiredState desired,
        ServiceRuntimeState runtime,
        bool deploymentActive,
        DateTime stateChangedAt,
        DateTime now,
        RestartPolicy restartPolicy = RestartPolicy.UnlessStopped)
    {
        if (deploymentActive || now - stateChangedAt < SettleTime)
            return DriftAction.None;

        return (desired, runtime) switch
        {
            // Run-to-completion containers (restart policy No, e.g. migrators) exit on their own; that is not drift.
            (DesiredState.Running, ServiceRuntimeState.Stopped or ServiceRuntimeState.Crashed) when restartPolicy == RestartPolicy.No => DriftAction.None,

            // OnFailure only restarts after a failure, so a clean exit (Stopped) is expected.
            (DesiredState.Running, ServiceRuntimeState.Stopped) when restartPolicy == RestartPolicy.OnFailure => DriftAction.None,

            // Wanted up but down. NotDeployed is excluded: there is nothing to start until it has been deployed once.
            (DesiredState.Running, ServiceRuntimeState.Stopped or ServiceRuntimeState.Crashed) => DriftAction.Start,

            // Wanted down but up, or stuck crashing/restarting. Stopping a crash-looping container is how the
            // operator breaks the loop, and it is idempotent when the container is already gone.
            (DesiredState.Stopped, ServiceRuntimeState.Running or ServiceRuntimeState.Crashed or ServiceRuntimeState.Restarting) => DriftAction.Stop,

            // Unknown is unobserved, and a restart wanted up is still settling; Docker events and the startup sync resolve them.
            _ => DriftAction.None
        };
    }
}