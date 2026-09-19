using Haven.Domain.Enums;

namespace Haven.Domain.Models;

/// <summary>
/// The outcome of a single health check run, including enough detail to explain a failure.
/// </summary>
/// <remarks>
/// <see cref="ServiceHealth.Unknown"/> means Haven could not perform the check (bad config, probe unavailable, Docker
/// error); only an observed failure of the target is <see cref="ServiceHealth.Unhealthy"/>.
/// </remarks>
public sealed record HealthCheckRunResult(
    ServiceHealth Status,
    HealthCheckFailureReason Reason,
    string? Message,
    long DurationMs,
    int? HttpStatusCode = null,
    long? ExitCode = null,
    string? Output = null,
    int Attempts = 1)
{
    public const int MaxOutputLength = 4096;

    public static HealthCheckRunResult Healthy(string? message = null, long durationMs = 0, int? httpStatusCode = null, long? exitCode = null, string? output = null) =>
        new(ServiceHealth.Healthy, HealthCheckFailureReason.None, message, durationMs, httpStatusCode, exitCode, Truncate(output));

    public static HealthCheckRunResult Unhealthy(HealthCheckFailureReason reason, string message, long durationMs = 0, int? httpStatusCode = null, long? exitCode = null, string? output = null) =>
        new(ServiceHealth.Unhealthy, reason, message, durationMs, httpStatusCode, exitCode, Truncate(output));

    public static HealthCheckRunResult Unknown(HealthCheckFailureReason reason, string message, long durationMs = 0, string? output = null) =>
        new(ServiceHealth.Unknown, reason, message, durationMs, Output: Truncate(output));

    public static string? Truncate(string? output)
    {
        if (string.IsNullOrEmpty(output))
            return null;

        return output.Length <= MaxOutputLength ? output : output[..MaxOutputLength] + "…";
    }
}
