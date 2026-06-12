using Hangfire;

using Microsoft.AspNetCore.Builder;

namespace Haven.Infrastructure.Extensions;

public static class HangfireServerExtensions
{
    public static IApplicationBuilder UseConfiguredHangfireServer(this IApplicationBuilder app)
    {
        return app.UseHangfireServer(new BackgroundJobServerOptions
        {
            Queues = new[] { "deployments", "default" },
            WorkerCount = 1
        });
    }
}