using System.Runtime.InteropServices;

using Docker.DotNet;

using Hangfire;
using Hangfire.PostgreSql;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Interfaces.SystemNotifications;
using Haven.Application.Configuration;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Auth;
using Haven.Infrastructure.BackgroundJobs;
using Haven.Infrastructure.Backup;
using Haven.Infrastructure.Configuration;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Deployment.Events;
using Haven.Infrastructure.Deployment.Git;
using Haven.Infrastructure.Notifications;
using Haven.Infrastructure.Notifications.Providers;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Persistence.Interceptors;
using Haven.Infrastructure.Persistence.Manifests;
using Haven.Infrastructure.Persistence.Repositories;
using Haven.Infrastructure.Persistence.Volumes;
using Haven.Infrastructure.Security;
using Haven.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        services.AddScoped<IHavenService, HavenService>();

        services.AddAuthServices();
        services.AddSecurityServices(configuration);
        services.AddPersistenceServices(connectionString);
        services.AddHavenConfigurationServices();
        services.AddEnvironmentVariableServices();
        services.AddManifestServices();
        services.AddDeploymentServices();
        services.AddManagedVolumeServices();
        services.AddGitServices();
        services.AddDockerServices();
        services.AddMediatorServices();
        services.AddNotificationServices();
        services.AddHangfireServices(connectionString);
        services.AddFuzzySearchableRepositories();
        services.AddSystemServices();
        services.AddBackupServices();
        services.AddHealthCheckServices();

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException(
                   "Connection string 'DefaultConnection' not found in configuration.");
    }

    private static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    private static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EncryptionOptions>(opts =>
            opts.Key = configuration[$"{EncryptionOptions.SectionName}:Key"] ?? string.Empty);
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }

    private static IServiceCollection AddPersistenceServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<HavenDbContext>(options =>
            options.UseNpgsql(connectionString)
        );
        services.AddScoped<DomainEventInterceptor>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<HavenDbContext>());
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<ISidecarRepository, SidecarRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEnvironmentVariableRepository, EnvironmentVariableRepository>();
        services.AddScoped<IHavenSettingRepository, HavenSettingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserInviteTokenRepository, UserInviteTokenRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.AddScoped<IGitCredentialsRepository, GitCredentialsRepository>();
        services.AddScoped<IServiceRegistryEntryRepository, ServiceRegistryEntryRepository>();
        services.AddScoped<ISslCertificateRepository, SslCertificateRepository>();
        services.AddScoped<ITraefikDynamicConfigWriter, TraefikDynamicConfigWriter>();
        services.AddScoped<INotificationChannelConfigRepository, NotificationChannelConfigRepository>();
        services.AddScoped<INotificationRuleRepository, NotificationRuleRepository>();
        services.AddScoped<IDeploymentRepository, DeploymentRepository>();
        services.AddScoped<INotificationAttemptRepository, NotificationAttemptRepository>();
        services.AddScoped<INotificationScopeResolver, NotificationScopeResolver>();

        return services;
    }

    private static IServiceCollection AddHavenConfigurationServices(this IServiceCollection services)
    {
        services.AddScoped<IHavenConfigurationSerializer, YamlHavenConfigurationSerializer>();
        services.AddScoped<IHavenConfigurationSeedService, HavenConfigurationSeedService>();
        services.AddSingleton<HavenConfigurationStore>();
        services.AddSingleton<IHavenConfigurationStore>(sp =>
            sp.GetRequiredService<HavenConfigurationStore>());

        services.AddHavenOptionsMonitor<ManifestsOptions>(ManifestsOptions.SectionName);
        services.AddHavenOptionsMonitor<InstanceOptions>(InstanceOptions.SectionName);
        services.AddHavenOptionsMonitor<NetworkOptions>(NetworkOptions.SectionName);
        services.AddHavenOptionsMonitor<SetupOptions>(SetupOptions.SectionName);
        services.AddHavenOptionsMonitor<BackupOptions>(BackupOptions.SectionName);
        services.AddHavenOptionsMonitor<TelemetryOptions>(TelemetryOptions.SectionName);
        services.AddHavenOptionsMonitor<VolumesOptions>(VolumesOptions.SectionName);
        services.AddHavenOptionsMonitor<GitHubAppOptions>(GitHubAppOptions.SectionName);
        services.AddHavenOptionsMonitor<DockerCleanupOptions>(DockerCleanupOptions.SectionName);
        services.AddHavenOptionsMonitor<RepositoryCleanupOptions>(RepositoryCleanupOptions.SectionName);
        services.AddHavenOptionsMonitor<TraefikOptions>(TraefikOptions.SectionName);

        return services;
    }

    private static IServiceCollection AddHavenOptionsMonitor<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class, new()
    {
        services.AddSingleton<IOptionsMonitor<TOptions>>(sp =>
            new HavenOptionsMonitor<TOptions>(
                sp.GetRequiredService<HavenConfigurationStore>(),
                sectionName));

        return services;
    }

    private static IServiceCollection AddEnvironmentVariableServices(this IServiceCollection services)
    {
        services.AddScoped<IEnvironmentVariableService, EnvironmentVariableService>();
        services.AddScoped<IEnvironmentVariableSerializer, EnvironmentVariableSerializer>();

        return services;
    }

    private static IServiceCollection AddManifestServices(this IServiceCollection services)
    {
        services.AddManifestSerializers();

        return services;
    }

    private static IServiceCollection AddManifestSerializers(this IServiceCollection services)
    {
        var genericSerializerInterface = typeof(IManifestSerializer<>);
        var genericParserInterface = typeof(IManifestParser<>);
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
                else if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericParserInterface)
                    services.AddScoped(iface, serializerType);
            }
        }

        return services;
    }

    private static IServiceCollection AddDeploymentServices(this IServiceCollection services)
    {
        services.AddSingleton<IHostPathResolver, DockerHostPathResolver>();
        services.AddScoped<IDockerContainerRuntime, DockerContainerRuntime>();
        services.AddScoped<ITraefikLabelMerger, TraefikLabelMerger>();
        services.AddScoped<IDeployService, DockerContainerDeployService>();
        services.AddScoped<IDeployService, DockerfileDeployService>();
        services.AddScoped<IDeployService, DockerSidecarDeployService>();
        services.AddScoped<IDeployServiceFactory, DeployServiceFactory>();
        services.AddScoped<IDeploymentJobEnqueuer, HangfireDeploymentJobEnqueuer>();
        services.AddScoped<IServiceCleanupJobEnqueuer, HangfireServiceCleanupJobEnqueuer>();
        services.AddScoped<IDeployWebhookService, DeployWebhookService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<IDeploymentOrchestrator, DeploymentOrchestrator>();
        services.AddSingleton<IDeploymentLogService, DeploymentLogService>();
        services.AddSingleton<IDeploymentCancellationService, DeploymentCancellationService>();
        services.AddScoped<IBuildInfoService, BuildInfoService>();
        services.AddScoped<IServiceRegistry, ServiceRegistry>();
        services.AddSingleton<IHavenEnvironment, HavenEnvironment>();

        return services;
    }

    private static IServiceCollection AddManagedVolumeServices(this IServiceCollection services)
    {
        services.AddScoped<IManagedVolumeFileService, ManagedVolumeFileService>();

        return services;
    }

    private static IServiceCollection AddGitServices(this IServiceCollection services)
    {
        var gitRepositoryRootPath = Path.Combine(AppContext.BaseDirectory, "git-repositories");
        services.AddSingleton<IGitRepositoryPathProvider>(new GitRepositoryPathProvider(gitRepositoryRootPath));
        services.AddScoped<IGitProviderFactory, GitProviderFactory>();
        services.AddScoped<IGitService, GitService>();
        services.AddScoped<IGitHubOAuthService, GitHubOAuthService>();
        services.AddHttpClient("github-oauth");
        services.AddMemoryCache();
        services.AddSingleton<IOAuthStateStore, MemoryOAuthStateStore>();

        return services;
    }

    private static IServiceCollection AddDockerServices(this IServiceCollection services)
    {
        services.AddSingleton<IDockerClient, DockerClient>(_ =>
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

        return services;
    }

    private static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
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
                typeof(Haven.Application.Common.Behaviors.ManifestSyncTriggerBehavior<,>),
                typeof(Haven.Application.Common.Behaviors.TransactionBehavior<,>),
            ];
        });

        return services;
    }

    private static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationEnqueuer, HangfireNotificationEnqueuer>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationProvider, WebhookNotificationProvider>();
        services.AddScoped<INotificationProvider, DiscordNotificationProvider>();
        services.AddScoped<INotificationProvider, SmtpNotificationProvider>();
        services.AddHttpClient("webhook");

        services.AddHttpClient<INotificationProvider, NtfyNotificationProvider>();

        // System (transactional) notifications — invites, future password recovery
        services.AddScoped<ISystemNotificationEnqueuer, HangfireSystemNotificationEnqueuer>();
        services.AddScoped<ISystemNotificationSender, SystemNotificationSender>();
        services.AddScoped<IFrontendLinkBuilder, FrontendLinkBuilder>();

        return services;
    }

    private static IServiceCollection AddHangfireServices(this IServiceCollection services, string connectionString)
    {
        services.AddHangfire(config => config.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
        services.AddScoped<IConfigurationWriteScheduler, HangfireConfigurationWriteScheduler>();
        services.AddHostedService<BackupSchedulerService>();
        services.AddHostedService<DeploymentLogCleanupSchedulerService>();
        services.AddScoped<IDockerCleanupService, DockerCleanupService>();
        services.AddHostedService<DockerCleanupSchedulerService>();
        services.AddHostedService<RepositoryCleanupSchedulerService>();
        services.AddScoped<INetworkReconciliationService, NetworkReconciliationService>();
        services.AddHostedService<NetworkReconciliationScheduler>();
        services.AddScoped<IJobsService, JobsService>();

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

    private static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        services.AddScoped<ISystemService, SystemService>();
        services.AddSingleton<IHavenRestartService, HavenRestartService>();

        return services;
    }

    private static IServiceCollection AddBackupServices(this IServiceCollection services)
    {
        services.AddScoped<IBackupManifestWriter, BackupManifestWriter>();
        services.AddScoped<IBackupManifestReader, BackupManifestReader>();
        services.AddSingleton<IBackupCoordinationLock, BackupCoordinationLock>();
        services.AddSingleton<ManifestSyncTrigger>();
        services.AddSingleton<IManifestSyncTrigger>(sp => sp.GetRequiredService<ManifestSyncTrigger>());
        services.AddHostedService<ManifestSyncBackgroundService>();

        return services;
    }

    private static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddScoped<IHealthCheckScheduler, HangfireHealthCheckScheduler>();
        services.AddScoped<IHealthCheckRunner, ContainerHealthCheckRunner>();
        services.AddScoped<IHealthCheckRunner, HttpHealthCheckRunner>();
        services.AddScoped<IHealthCheckRunner, BashHealthCheckRunner>();
        services.AddScoped<IHealthCheckRunnerFactory, HealthCheckRunnerFactory>();

        services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();

        services.AddHttpClient(nameof(HttpHealthCheckRunner));
        services.AddHostedService<HealthCheckSchedulerStartupService>();

        services.AddScoped<ITraefikApiClient, TraefikApiClient>();
        services.AddHttpClient(nameof(TraefikApiClient));

        return services;
    }
}
