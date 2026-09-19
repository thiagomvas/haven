namespace Haven.Application.Common.Interfaces.Services;

/// <param name="NetworkName">Docker network the probe container joins (the target's own network).</param>
/// <param name="Entrypoint">Binary of the probe image to run (<c>curl</c> for HTTP, <c>nc</c> for TCP).</param>
/// <param name="Args">Arguments passed to <paramref name="Entrypoint"/>.</param>
/// <param name="Timeout">How long the probe command itself is allowed to run.</param>
public sealed record ProbeRequest(string NetworkName, string Entrypoint, IReadOnlyList<string> Args, TimeSpan Timeout);

public sealed record ProbeOutput(long ExitCode, string StdOut, string StdErr, long DurationMs);

/// <summary>
/// Runs a short-lived container attached only to a service's environment network, so checks can reach the service
/// by container name without Haven itself joining that network. A failed <see cref="Result{T}"/> means Haven could
/// not run the probe at all, which is different from the probed target being unhealthy.
/// </summary>
public interface IHealthCheckProbe
{
    Task<Result<ProbeOutput>> RunAsync(ProbeRequest request, CancellationToken cancellationToken = default);
}
