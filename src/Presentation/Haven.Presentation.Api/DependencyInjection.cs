using Haven.Application.Common.Interfaces.Hubs;
using Haven.Presentation.Api.Hubs;
using Haven.Presentation.Api.Services;

namespace Haven.Presentation.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSignalRServices();

        return services;
    }

    public static WebApplication MapHavenHubs(this WebApplication app)
    {
        app.MapHub<ServiceStatusHub>("/hubs/services/status");
        return app;
    }

    private static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR();

        services.AddScoped<IServiceStatusNotifier, SignalrServiceStatusNotifier>();
        return services;
    }
}