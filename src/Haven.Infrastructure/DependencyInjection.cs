using System.Runtime.InteropServices;

using Docker.DotNet;

using Hangfire;
using Hangfire.Storage.SQLite;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Auth;
using Haven.Infrastructure.BackgroundJobs;
using Haven.Infrastructure.Configuration;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Deployment.Events;
using Haven.Infrastructure.Deployment.Git;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Persistence.Manifests;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Infrastructure.Security;
using Haven.Infrastructure.Backup;
using Haven.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IHavenService, HavenService>();
        // Auth
        services.AddScoped<IAuthService, AuthService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Security
        services.Configure<EncryptionOptions>(opts =>
            opts.Key = configuration[$"{EncryptionOptions.SectionName}:Key"] ?? string.Empty);
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // Data Access
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' not found in configuration.");

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.AddScoped<IGitCredentialsRepository, GitCredentialsRepository>();

        // Configuration
        services.AddScoped<IHavenConfigurationSerializer, YamlHavenConfigurationSerializer>();
        services.AddScoped<IHavenConfigurationSeedService, HavenConfigurationSeedService>();
        services.AddSingleton<HavenConfigurationStore>();
        services.AddSingleton<IHavenConfigurationStore>(sp =>
            sp.GetRequiredService<HavenConfigurationStore>());
        services.AddSingleton<IOptionsMonitor<ManifestsOptions>>(sp =>
            new HavenOptionsMonitor<ManifestsOptions>(
                sp.GetRequiredService<HavenConfigurationStore>(),
                ManifestsOptions.SectionName));
        services.AddSingleton<IOptionsMonitor<InstanceOptions>>(sp =>
            new HavenOptionsMonitor<InstanceOptions>(
                sp.GetRequiredService<HavenConfigurationStore>(),
                InstanceOptions.SectionName));
        services.AddSingleton<IOptionsMonitor<NetworkOptions>>(sp =>
            new HavenOptionsMonitor<NetworkOptions>(
                sp.GetRequiredService<HavenConfigurationStore>(),
                NetworkOptions.SectionName));
        services.AddSingleton<IOptionsMonitor<SetupOptions>>(sp =>
            new HavenOptionsMonitor<SetupOptions>(
                sp.GetRequiredService<HavenConfigurationStore>(),
                SetupOptions.SectionName));
        services.AddSingleton<IOptionsMonitor<BackupOptions>>(sp =>
            new HavenOptionsMonitor<BackupOptions>(
                sp.GetRequiredService<HavenConfigurationStore>(),
                BackupOptions.SectionName));

        services.AddScoped<IEnvironmentVariableService, EnvironmentVariableService>();
        // Manifests
        services.AddScoped<IManifestSerializer, YamlManifestSerializer>();
        services.AddScoped<IManifestSyncService, ManifestSyncOrchestrator>();
        services.AddManifestSerializers();
        services.AddScoped<IEnvironmentVariableSerializer, EnvironmentVariableSerializer>();

        // Deployment
        services.AddScoped<IDeployService, DockerContainerDeployService>();
        services.AddScoped<IDeployService, DockerfileDeployService>();
        services.AddScoped<IDeployServiceFactory, DeployServiceFactory>();
        services.AddScoped<IDeploymentJobEnqueuer, HangfireDeploymentJobEnqueuer>();
        services.AddScoped<IDeployWebhookService, DeployWebhookService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<IDeploymentOrchestrator, DeploymentOrchestrator>();
        services.AddScoped<IBuildInfoService, BuildInfoService>();

        // Git Services
        var gitRepositoryRootPath = Path.Combine(AppContext.BaseDirectory, "git-repositories");
        services.AddSingleton<IGitRepositoryPathProvider>(new GitRepositoryPathProvider(gitRepositoryRootPath));
        services.AddScoped<IGitProviderFactory, GitProviderFactory>();
        services.AddScoped<IGitService, GitService>();

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
            options.Assemblies =
            [
                typeof(DependencyInjection).Assembly, // Infrastructure
                typeof(Haven.Application.DependencyInjection).Assembly, // Application
                typeof(Haven.Domain.Aggregates.Project).Assembly // Domain
            ];
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors =
            [
                typeof(Haven.Application.Common.Behaviors.LoggingBehavior<,>),
                typeof(Haven.Application.Common.Behaviors.PermissionBehavior<,>),
                typeof(Haven.Application.Common.Behaviors.ValidationBehavior<,>),
                typeof(Haven.Application.Common.Behaviors.TransactionBehavior<,>)
            ];
        });

        // Hangfire
        services.AddHangfire(config => config.UseSQLiteStorage());
        services.AddHostedService<BackupSchedulerService>();
        services.AddFuzzySearchableRepositories();

        services.AddScoped<ISystemService, SystemService>();
        services.AddSingleton<IHavenRestartService, HavenRestartService>();

        // Backup
        services.AddScoped<IBackupManifestWriter, BackupManifestWriter>();

        return services;
    }

    private static IServiceCollection AddManifestSerializers(this IServiceCollection services)
    {
        var genericSerializerInterface = typeof(IManifestSerializer<>);
        var entitySerializerInterface = typeof(IManifestEntitySerializer);

        var serializerTypes = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && entitySerializerInterface.IsAssignableFrom(t));

        foreach (var serializerType in serializerTypes)
        {
            services.AddScoped(entitySerializerInterface, serializerType);

            foreach (var iface in serializerType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericSerializerInterface)
                    services.AddScoped(iface, serializerType);
            }
        }

        return services;
    }

    private static IServiceCollection AddFuzzySearchableRepositories(this IServiceCollection services)
    {
        var repositoryInterfaceType = typeof(IFuzzySearchableRepository);
        var repositoryTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => repositoryInterfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        foreach (var repoType in repositoryTypes)
        {
            services.AddScoped(typeof(IFuzzySearchableRepository), repoType);
        }

        services.AddScoped<IFuzzySearchService, FuzzySearchService>();

        return services;
    }
}