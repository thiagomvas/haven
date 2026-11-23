namespace Haven.Presentation.Api.Bootstrapping;

/// <summary>
/// Ports Haven listens on. The webhook port is meant to be exposed externally and only serves
/// <see cref="WebhookRoutes.Prefix"/> routes (enforced by the webhook port gate middleware).
/// </summary>
public sealed record WebhookPortOptions(int MainPort, int WebhookPort)
{
    public const int DefaultMainPort = 8080;
    public const int DefaultWebhookPort = 8888;

    public static WebhookPortOptions From(IConfiguration configuration)
    {
        var main = ReadPort(configuration, "PORT", DefaultMainPort);
        var webhook = ReadPort(configuration, "WEBHOOK_PORT", DefaultWebhookPort);

        if (main == webhook)
            throw new InvalidOperationException(
                $"PORT and WEBHOOK_PORT must differ (both are {main}); the webhook port only serves webhook routes.");

        return new WebhookPortOptions(main, webhook);
    }

    private static int ReadPort(IConfiguration configuration, string key, int fallback)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw)) return fallback;

        if (!int.TryParse(raw, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException($"{key} must be a port between 1 and 65535, got '{raw}'.");

        return port;
    }
}
