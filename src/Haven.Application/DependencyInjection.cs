using FluentValidation;

using Haven.Application.Common.Behaviors;
using Haven.Application.Common.Telemetry;
using Haven.Domain.Events;

using Mediator;

using Microsoft.Extensions.DependencyInjection;

namespace Haven.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddSingleton<HavenMetrics>();

        return services;
    }
}