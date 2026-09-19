using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.HealthChecks;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Models;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services;

public class HttpHealthCheckRunner : IHealthCheckRunner
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHealthCheckProbe _probe;
    private readonly IHealthCheckTargetResolver _targetResolver;
    private readonly ILogger<HttpHealthCheckRunner> _logger;

    public HttpHealthCheckRunner(
        IHttpClientFactory httpClientFactory,
        IHealthCheckProbe probe,
        IHealthCheckTargetResolver targetResolver,
        ILogger<HttpHealthCheckRunner> logger)
    {
        _httpClientFactory = httpClientFactory;
        _probe = probe;
        _targetResolver = targetResolver;
        _logger = logger;
    }

    public HealthCheckKind Kind => HealthCheckKind.Http;

    public async Task<HealthCheckRunResult> RunHealthCheckAsync(HealthCheck healthCheck, CancellationToken cancellationToken = default)
    {
        HttpHealthCheckConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<HttpHealthCheckConfig>(healthCheck.Config, HealthCheckConfigValidator.JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid config for health check '{HealthCheckId}'", healthCheck.Id);
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, $"The health check configuration is not valid JSON: {ex.Message}");
        }

        if (config is null || string.IsNullOrWhiteSpace(config.Url))
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, "The health check has no URL configured.");

        var timeoutSeconds = Math.Max(1, config.TimeoutSeconds);

        return config.Mode == HttpHealthCheckMode.Probe
            ? await RunViaProbeAsync(healthCheck, config, timeoutSeconds, cancellationToken)
            : await RunDirectAsync(config, timeoutSeconds, cancellationToken);
    }

    private async Task<HealthCheckRunResult> RunViaProbeAsync(HealthCheck healthCheck, HttpHealthCheckConfig config, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var target = await _targetResolver.ResolveAsync(healthCheck.ServiceId, cancellationToken);
        if (target.IsFailure)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.ContainerNotFound, "No container exists for this service yet, so there is nothing to check.");

        if (!target.Value.IsRunning)
            return HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.ContainerNotRunning, $"Container '{target.Value.ContainerName}' is not running.");

        if (target.Value.NetworkName is null)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.ProbeUnavailable, $"Container '{target.Value.ContainerName}' is not attached to a network the probe can join.");

        var url = HealthCheckPlaceholders.Apply(config.Url, target.Value, out var placeholderError);
        if (url is null)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, placeholderError!);

        var output = await _probe.RunAsync(
            new ProbeRequest(target.Value.NetworkName, ProbeCommands.HttpEntrypoint, ProbeCommands.BuildHttpArgs(config.Method, url, timeoutSeconds), TimeSpan.FromSeconds(timeoutSeconds)),
            cancellationToken);

        if (output.IsFailure)
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.ProbeUnavailable, output.Error.Message);

        return ProbeCommands.ParseHttp(output.Value, config.ExpectedStatusCodes, url);
    }

    private async Task<HealthCheckRunResult> RunDirectAsync(HttpHealthCheckConfig config, int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (HealthCheckPlaceholders.ContainsPlaceholder(config.Url))
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, "URL placeholders such as {{container}} require the Probe mode.");

        var client = _httpClientFactory.CreateClient(nameof(HttpHealthCheckRunner));
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(config.Method), config.Url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            stopwatch.Stop();

            var statusCode = (int)response.StatusCode;
            var expectedCodes = config.ExpectedStatusCodes is { Length: > 0 } ? config.ExpectedStatusCodes : [200];
            return expectedCodes.Contains(statusCode)
                ? HealthCheckRunResult.Healthy($"HTTP {statusCode} from {config.Url}", stopwatch.ElapsedMilliseconds, statusCode)
                : HealthCheckRunResult.Unhealthy(
                    HealthCheckFailureReason.UnexpectedStatusCode,
                    $"Expected HTTP {string.Join("/", expectedCodes)} but received {statusCode} from {config.Url}",
                    stopwatch.ElapsedMilliseconds,
                    statusCode);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckRunResult.Unhealthy(HealthCheckFailureReason.Timeout, $"Timed out after {timeoutSeconds}s waiting for {config.Url}", stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            return ClassifyTransportFailure(ex, config.Url, stopwatch.ElapsedMilliseconds);
        }
        catch (UriFormatException ex)
        {
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, $"The URL is not valid: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            return HealthCheckRunResult.Unknown(HealthCheckFailureReason.InvalidConfig, $"The request could not be built: {ex.Message}");
        }
    }

    private static HealthCheckRunResult ClassifyTransportFailure(HttpRequestException ex, string url, long durationMs)
    {
        var socketError = (ex.InnerException as SocketException)?.SocketErrorCode;

        var reason = ex.HttpRequestError switch
        {
            HttpRequestError.NameResolutionError => HealthCheckFailureReason.DnsFailure,
            HttpRequestError.SecureConnectionError => HealthCheckFailureReason.TlsError,
            HttpRequestError.ConnectionError when socketError is SocketError.HostNotFound or SocketError.TryAgain => HealthCheckFailureReason.DnsFailure,
            HttpRequestError.ConnectionError => HealthCheckFailureReason.ConnectionRefused,
            _ => HealthCheckFailureReason.Error
        };

        var detail = ex.InnerException?.Message ?? ex.Message;
        var message = reason switch
        {
            HealthCheckFailureReason.DnsFailure => $"Could not resolve the host of {url}: {detail}",
            HealthCheckFailureReason.ConnectionRefused => $"Could not connect to {url}: {detail}",
            HealthCheckFailureReason.TlsError => $"TLS error reaching {url}: {detail}",
            _ => $"Request to {url} failed: {detail}"
        };

        return HealthCheckRunResult.Unhealthy(reason, message, durationMs);
    }
}
