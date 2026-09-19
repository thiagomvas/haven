using System.Globalization;
using System.Text.Json;

using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Enums;
using Haven.Domain.Models;

namespace Haven.Infrastructure.Services;

/// <summary>
/// Builds the curl (HTTP) and nc (TCP) invocations run inside the probe container and turns their output and exit
/// code into a health check result.
/// </summary>
public static class ProbeCommands
{
    public const string HttpEntrypoint = "curl";
    public const string TcpEntrypoint = "nc";
    public const string BodySeparator = "\n---HAVEN---\n";
    private const long MaxBodyBytes = 5 * 1024 * 1024;

    public static IReadOnlyList<string> BuildHttpArgs(string method, string url, int timeoutSeconds) =>
    [
        "-sS",
        "-X", method,
        "--max-time", timeoutSeconds.ToString(CultureInfo.InvariantCulture),
        "--max-filesize", MaxBodyBytes.ToString(CultureInfo.InvariantCulture),
        "-o", "-",
        "-w", BodySeparator + "%{json}",
        "--", url
    ];

    public static IReadOnlyList<string> BuildTcpArgs(string host, int port, int timeoutSeconds) =>
    [
        "-z",
        "-w", timeoutSeconds.ToString(CultureInfo.InvariantCulture),
        host,
        port.ToString(CultureInfo.InvariantCulture)
    ];

    public static HealthCheckRunResult ParseHttp(ProbeOutput output, int[] expectedStatusCodes, string url)
    {
        var (body, json) = SplitOutput(output.StdOut);
        var statusCode = 0;
        var durationMs = output.DurationMs;

        if (json is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("response_code", out var code) && code.TryGetInt32(out var parsedCode))
                    statusCode = parsedCode;

                if (statusCode > 0 && doc.RootElement.TryGetProperty("time_total", out var time) && time.TryGetDouble(out var seconds))
                    durationMs = (long)(seconds * 1000);
            }
            catch (JsonException)
            {
                // Treated as a transport failure below.
            }
        }

        // Exit 63 means the body exceeded the size limit; the status line was still received and is what we judge.
        if (statusCode > 0 && output.ExitCode is 0 or 63)
        {
            var expected = expectedStatusCodes is { Length: > 0 } ? expectedStatusCodes : [200];
            return expected.Contains(statusCode)
                ? HealthCheckRunResult.Healthy($"HTTP {statusCode} from {url}", durationMs, statusCode, output.ExitCode, body)
                : HealthCheckRunResult.Unhealthy(
                    HealthCheckFailureReason.UnexpectedStatusCode,
                    $"Expected HTTP {string.Join("/", expected)} but received {statusCode} from {url}",
                    durationMs, statusCode, output.ExitCode, body);
        }

        return FromCurlFailure(output, output.DurationMs, url, body);
    }

    public static HealthCheckRunResult ParseTcp(ProbeOutput output, string host, int port, int timeoutSeconds)
    {
        var target = $"{host}:{port}";
        if (output.ExitCode == 0)
            return HealthCheckRunResult.Healthy($"Connected to {target}", output.DurationMs, exitCode: 0);

        var detail = output.StdErr.Trim();
        var (reason, message) = detail.Contains("bad address", StringComparison.OrdinalIgnoreCase)
            ? (HealthCheckFailureReason.DnsFailure, $"Could not resolve the host of {target}")
            : output.DurationMs >= timeoutSeconds * 1000L - 250
                ? (HealthCheckFailureReason.Timeout, $"Timed out after {timeoutSeconds}s connecting to {target}")
                : (HealthCheckFailureReason.ConnectionRefused, $"Could not connect to {target} (connection refused or unreachable)");

        return HealthCheckRunResult.Unhealthy(reason, message, output.DurationMs, exitCode: output.ExitCode,
            output: string.IsNullOrEmpty(detail) ? null : detail);
    }

    private static HealthCheckRunResult FromCurlFailure(ProbeOutput output, long durationMs, string target, string? body)
    {
        var detail = output.StdErr.Trim();
        var reason = output.ExitCode switch
        {
            6 => HealthCheckFailureReason.DnsFailure,
            7 => HealthCheckFailureReason.ConnectionRefused,
            28 => HealthCheckFailureReason.Timeout,
            35 or 51 or 58 or 59 or 60 or 77 or 83 or 90 or 91 => HealthCheckFailureReason.TlsError,
            _ => HealthCheckFailureReason.Error
        };

        var summary = reason switch
        {
            HealthCheckFailureReason.DnsFailure => $"Could not resolve the host of {target}",
            HealthCheckFailureReason.ConnectionRefused => $"Could not connect to {target} (connection refused or unreachable)",
            HealthCheckFailureReason.Timeout => $"Timed out reaching {target}",
            HealthCheckFailureReason.TlsError => $"TLS error reaching {target}",
            _ => $"Request to {target} failed (curl exit code {output.ExitCode})"
        };

        return HealthCheckRunResult.Unhealthy(reason, summary, durationMs, exitCode: output.ExitCode,
            output: string.IsNullOrEmpty(detail) ? body : detail);
    }

    private static (string Body, string? Json) SplitOutput(string stdout)
    {
        var index = stdout.LastIndexOf(BodySeparator, StringComparison.Ordinal);
        return index < 0
            ? (stdout, null)
            : (stdout[..index], stdout[(index + BodySeparator.Length)..]);
    }
}
