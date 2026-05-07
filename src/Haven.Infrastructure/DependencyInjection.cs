using System.Runtime.InteropServices;
using Docker.DotNet;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Infrastructure.Configuration;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Deployment.Events;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Infrastructure.Security;
using Haven.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Security
        services.Configure<EncryptionOptions>(opts =>
            opts.Key = configuration[$"{EncryptionOptions.SectionName}:Key"] ?? string.Empty);
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // Data Access
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

        services.AddDbContext<HavenDbContext>(options =>
            options.UseSqlite(connectionString)
        );
        services.AddScoped<DomainEventInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HavenDbContext>());
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEnvironmentVariableRepository, EnvironmentVariableRepository>();
        services.AddScoped<IHavenSettingRepository, HavenSettingRepository>();

        // Configuration
        services.AddSingleton<HavenConfigurationStore>();
        services.AddSingleton<IHavenConfigurationStore>(sp =>
            sp.GetRequiredService<HavenConfigurationStore>());
        services.AddSingleton<IOptionsMonitor<ManifestsOptions>>(sp =>
            new HavenOptionsMonitor<ManifestsOptions>(
                sp.GetRequiredService<HavenConfigurationStore>(),
                ManifestsOptions.SectionName));

        services.AddScoped<IEnvironmentVariableService, EnvironmentVariableService>();
        // Manifests
        services.AddScoped<IManifestSerializer, YamlManifestSerializer>();
        services.AddHostedService<ManifestSyncService>();
        services.AddScoped<IEnvironmentVariableSerializer, EnvironmentVariableSerializer>();

        // Deployment
        services.AddScoped<IDeployService, DockerContainerDeployService>();
        services.AddScoped<IDeployServiceFactory, DeployServiceFactory>();

        services.AddSingleton<IDockerClient, DockerClient>(sp =>
        {
            var uri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "npipe://./pipe/docker_engine"
                : "unix:///var/run/docker.sock";

            return new DockerClientConfiguration(new Uri(uri)).CreateClient();
        });
        services.AddSingleton<IDockerEventParser, DockerEventParser>();

        services.AddHostedService<ContainerStateSyncService>();
        services.AddHostedService<ContainerMonitoringJobService>();

        services.AddScoped<INetworkingServiceFactory, NetworkingServiceFactory>();
        services.AddScoped<INetworkingService, DockerNetworkingService>();

        services.AddMediator(options =>
        {
            options.Assemblies = [
                typeof(DependencyInjection).Assembly,  // Infrastructure
                typeof(Haven.Application.DependencyInjection).Assembly,  // Application
                typeof(Haven.Domain.Aggregates.Project).Assembly  // Domain
            ];
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [
                typeof(Haven.Application.Common.Behaviors.LoggingBehavior<,>),
                typeof(Haven.Application.Common.Behaviors.ValidationBehavior<,>),
                typeof(Haven.Application.Common.Behaviors.TransactionBehavior<,>)
            ];
        });

        return services;
    }
}
