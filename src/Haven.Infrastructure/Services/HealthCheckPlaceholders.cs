namespace Haven.Infrastructure.Services;

/// <summary>Resolves <c>{{container}}</c> and <c>{{port}}</c> in health check URLs/hosts against the target container.</summary>
internal static class HealthCheckPlaceholders
{
    public const string Container = "{{container}}";
    public const string Port = "{{port}}";

    public static bool ContainsPlaceholder(string value) => value.Contains("{{", StringComparison.Ordinal);

    /// <returns>The resolved value, or null with <paramref name="error"/> set when a placeholder can't be resolved.</returns>
    public static string? Apply(string template, HealthCheckTarget target, out string? error)
    {
        error = null;
        var value = template.Replace(Container, target.ContainerName, StringComparison.OrdinalIgnoreCase);

        if (value.Contains(Port, StringComparison.OrdinalIgnoreCase))
        {
            if (target.Port is null)
            {
                error = "The URL uses {{port}} but the container does not expose any port.";
                return null;
            }

            value = value.Replace(Port, target.Port.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        if (ContainsPlaceholder(value))
        {
            error = "The URL contains an unknown placeholder. Supported placeholders are {{container}} and {{port}}.";
            return null;
        }

        return value;
    }
}
