using System.Text.Json.Serialization;

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
        app.MapHub<ServiceStatusHub>("/hubs/services/status").RequireCors();
        app.MapHub<DeploymentLogHub>("/hubs/deployments/logs").RequireCors();
        app.MapHub<ContainerShellHub>("/hubs/services/shell").RequireCors();
        return app;
    }

    private static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                // SignalR's JSON hub protocol has its own serializer options, separate from the
                // one FastEndpoints uses for REST — without this, enum args (e.g. ContainerShellHub's
                // ShellType) are expected/emitted as numbers instead of the string names the
                // frontend sends.
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddScoped<IServiceStatusNotifier, SignalrServiceStatusNotifier>();
        services.AddSingleton<IDeploymentLogNotifier, SignalrDeploymentLogNotifier>();
        services.AddSingleton<IContainerShellNotifier, SignalrContainerShellNotifier>();
        services.AddSingleton<ContainerShellSessionManager>();
        return services;
    }
}