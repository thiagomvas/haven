using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Haven.Presentation.Api.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireJobScheduling(this IServiceCollection services)
    {
        services.AddHangfire(config => config.UseInMemoryStorage());
        services.AddHangfireServer();

        return services;
    }

    public static IApplicationBuilder UseHangfireJobScheduling(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard();

        return app;
    }
}
