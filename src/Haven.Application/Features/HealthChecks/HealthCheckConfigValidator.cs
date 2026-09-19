using System.Text.Json;
using System.Text.Json.Serialization;

using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.HealthChecks;

public static class HealthCheckConfigValidator
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static bool IsValid(HealthCheckKind kind, string? config) => kind switch
    {
        HealthCheckKind.Container => true,
        HealthCheckKind.Http => TryDeserialize<HttpHealthCheckConfig>(config, out var http) && !string.IsNullOrWhiteSpace(http.Url),
        HealthCheckKind.Bash => TryDeserialize<BashHealthCheckConfig>(config, out var bash) && !string.IsNullOrWhiteSpace(bash.Command),
        HealthCheckKind.Tcp => TryDeserialize<TcpHealthCheckConfig>(config, out var tcp) && !string.IsNullOrWhiteSpace(tcp.Host) && tcp.Port is > 0 and <= 65535,
        _ => false
    };

    private static bool TryDeserialize<T>(string? config, out T value) where T : new()
    {
        if (string.IsNullOrWhiteSpace(config))
        {
            value = new T();
            return false;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<T>(config, JsonOptions);
            if (deserialized is null)
            {
                value = new T();
                return false;
            }

            value = deserialized;
            return true;
        }
        catch (JsonException)
        {
            value = new T();
            return false;
        }
    }
}

public enum HttpHealthCheckMode
{
    /// <summary>Request is made by Haven itself; only reaches published ports and public domains. The legacy behaviour.</summary>
    Direct,

    /// <summary>Request is made from a short-lived container on the service's environment network.</summary>
    Probe
}

public class HttpHealthCheckConfig
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public int[] ExpectedStatusCodes { get; set; } = [200];
    public int TimeoutSeconds { get; set; } = 5;
    public HttpHealthCheckMode Mode { get; set; } = HttpHealthCheckMode.Direct;
}

public class BashHealthCheckConfig
{
    public string Command { get; set; } = string.Empty;
    public int ExpectedExitCode { get; set; }
    public int TimeoutSeconds { get; set; } = 5;
}

public class TcpHealthCheckConfig
{
    /// <summary>Host to connect to. Supports the <c>{{container}}</c> placeholder.</summary>
    public string Host { get; set; } = "{{container}}";

    /// <summary>Port to connect to (1-65535).</summary>
    public int Port { get; set; }

    public int TimeoutSeconds { get; set; } = 5;
}
