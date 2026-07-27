using System.Text.Json;

using Haven.Domain;

namespace Haven.Application.Features.HealthChecks;

public static class HealthCheckConfigValidator
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool IsValid(HealthCheckKind kind, string? config) => kind switch
    {
        HealthCheckKind.Container => true,
        HealthCheckKind.Http => TryDeserialize<HttpHealthCheckConfig>(config, out var http) && !string.IsNullOrWhiteSpace(http.Url),
        HealthCheckKind.Bash => TryDeserialize<BashHealthCheckConfig>(config, out var bash) && !string.IsNullOrWhiteSpace(bash.Command),
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

public class HttpHealthCheckConfig
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public int[] ExpectedStatusCodes { get; set; } = [200];
    public int TimeoutSeconds { get; set; } = 5;
}

public class BashHealthCheckConfig
{
    public string Command { get; set; } = string.Empty;
    public int ExpectedExitCode { get; set; }
    public int TimeoutSeconds { get; set; } = 5;
}
