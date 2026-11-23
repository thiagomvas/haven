using Haven.Presentation.Api.Bootstrapping;

namespace Haven.Presentation.Api.Middleware;

/// <summary>
/// The webhook port is exposed externally, so it must never serve anything but webhook routes.
/// </summary>
public sealed class WebhookPortGateMiddleware(
    RequestDelegate next,
    WebhookPortOptions options,
    ILogger<WebhookPortGateMiddleware> logger)
{
    private static readonly PathString WebhookPrefix = new(WebhookRoutes.Prefix);

    public async Task InvokeAsync(HttpContext context)
    {
        var isWebhookPort = context.Connection.LocalPort == options.WebhookPort;

        if (isWebhookPort && !context.Request.Path.StartsWithSegments(WebhookPrefix))
        {
            logger.LogDebug(
                "Blocked non-webhook request {Method} {Path} on webhook port {Port} from {RemoteIp}",
                context.Request.Method,
                context.Request.Path,
                options.WebhookPort,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context);
    }
}