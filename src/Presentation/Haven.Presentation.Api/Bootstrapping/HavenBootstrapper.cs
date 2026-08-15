using System.Text;

using FastEndpoints;
using FastEndpoints.Swagger;

using Haven.Application;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure;
using Haven.Infrastructure.Configuration;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Notifications;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;
using Haven.Presentation.Api.Cors;
using Haven.Presentation.Api.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Events;

using TelemetryOptions = Haven.Application.Configuration.TelemetryOptions;

namespace Haven.Presentation.Api.Bootstrapping;

/// <summary>
/// Two-phase application bootstrap: <see cref="ConfigureHavenServices"/> runs before
/// <see cref="WebApplicationBuilder.Build"/> (Kestrel/auth/telemetry/DI wiring), and
/// <see cref="RunHavenStartupTasksAsync"/> runs after <c>Build()</c> but before <c>app.Run()</c>
/// (migrations, one-time data seeding/migration, and scheduling background init work). HTTP
/// pipeline/endpoint composition (<c>app.Use...</c>/<c>app.Map...</c>) stays in Program.cs since
/// that ordering is meant to be read there, not hidden behind a helper call.
/// </summary>
public static class HavenBootstrapper
{
    /// <summary>
    /// Pre-init: registers everything the app needs before it can be built. Returns the resolved
    /// <see cref="TelemetryOptions"/> since Program.cs also needs it after <c>Build()</c> to decide
    /// whether to map the Prometheus scraping endpoint.
    /// </summary>
    public static TelemetryOptions ConfigureHavenServices(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080);
            if (builder.Environment.IsDevelopment())
            {
                options.ListenAnyIP(8443, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
            }
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSection = builder.Configuration.GetSection("Jwt");
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["Secret"]!))
                };
            });
        builder.Services.AddAuthorization();

        var telemetryOptions = TelemetryStartupReader.Read(
            builder.Configuration.GetConnectionString("DefaultConnection"));

        if (telemetryOptions.Enabled)
            builder.ConfigureOpenTelemetry(telemetryOptions);

        builder.Host.UseSerilog((_, config) =>
        {
            config
                .WriteTo.Console()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Haven.Presentation.Api");

            config.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
            config.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
            config.MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Warning);
        });

        builder.Services.AddCors();
        builder.Services.AddSingleton<ICorsPolicyProvider, DynamicCorsPolicyProvider>();

        builder.Services.AddApplication();
        builder.Services.AddPresentation();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<TimezoneAwareDateTimeOffsetConverter>();
        builder.Services.AddSingleton<TimezoneAwareDateTimeConverter>();
        builder.Services.AddFastEndpoints()
            .SwaggerDocument(o =>
            {
                o.AutoTagPathSegmentIndex = 0;
                o.ShortSchemaNames = true;
            });

        return telemetryOptions;
    }

    private static void ConfigureOpenTelemetry(this WebApplicationBuilder builder, TelemetryOptions telemetryOptions)
    {
        var serviceName = string.IsNullOrWhiteSpace(telemetryOptions.ServiceName)
            ? "Haven"
            : telemetryOptions.ServiceName;

        var otlpProtocol = telemetryOptions.Protocol == Haven.Application.Configuration.OtlpProtocol.Grpc
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

        var otlpEndpoint = string.IsNullOrWhiteSpace(telemetryOptions.OtlpEndpoint)
            ? "http://localhost:4317"
            : telemetryOptions.OtlpEndpoint;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpEndpoint);
                    o.Protocol = otlpProtocol;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(Haven.Application.Common.Telemetry.HavenMetrics.MeterName)
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpEndpoint);
                    o.Protocol = otlpProtocol;
                })
                .AddPrometheusExporter());
    }

    /// <summary>
    /// Post-init: one-time startup work that needs a built <see cref="WebApplication"/> and a DI
    /// scope. Skipped entirely under the
    /// "Testing" environment, matching the previous inline behavior in Program.cs.
    /// </summary>
    public static async Task RunHavenStartupTasksAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
            return;

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<HavenDbContext>();
        context.Database.Migrate();

        var encryptionService = services.GetRequiredService<IEncryptionService>();
        var startupLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        await SmtpPasswordMigrator.EncryptLegacyPasswordsAsync(context, encryptionService, startupLogger);

        var seedService = services.GetRequiredService<IHavenConfigurationSeedService>();
        await seedService.SeedAsync(CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var optionsMonitor = services
            .GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Haven.Application.Configuration.ManifestsOptions>>();
        PathResolver.Initialize(optionsMonitor);

        var scheduler = services.GetRequiredService<IConfigurationWriteScheduler>();
        scheduler.ScheduleWrite();

        await EnsureSystemNetworkAsync(services, startupLogger, CancellationToken.None);
    }

    private static async Task EnsureSystemNetworkAsync(IServiceProvider services, Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken)
    {
        var networkRepository = services.GetRequiredService<INetworkRepository>();
        var network = (await networkRepository.GetAllAsync(NetworkType.System, cancellationToken)).FirstOrDefault();

        if (network is null)
        {
            network = Network.CreateSystemNetwork();
            await networkRepository.AddAsync(network, cancellationToken);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        }

        var networkingServiceFactory = services.GetRequiredService<INetworkingServiceFactory>();
        var networkingService = networkingServiceFactory.Create(ServiceType.DockerImage)
            ?? throw new InvalidOperationException($"No Docker networking service registered; cannot create the required '{DomainConstants.SystemNetworkName}' network.");

        var result = await networkingService.EnsureNetworkExistsAsync(network.Id, cancellationToken);
        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to create the required '{DomainConstants.SystemNetworkName}' network: {result.Error}");

        logger.LogInformation("'{NetworkName}' control-plane network is ready", DomainConstants.SystemNetworkName);

        await ConnectSelfToSystemNetworkAsync(services, network, logger, cancellationToken);
    }

    private static async Task ConnectSelfToSystemNetworkAsync(IServiceProvider services, Network network, Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(network.DockerNetworkId))
            return;

        var containerRuntime = services.GetRequiredService<IDockerContainerRuntime>();
        var selfContainerId = System.Environment.MachineName;

        var result = await containerRuntime.ConnectContainerToNetworkAsync(selfContainerId, network.DockerNetworkId, cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("Could not connect Haven's own container '{ContainerId}' to '{NetworkName}': {Error}", selfContainerId, DomainConstants.SystemNetworkName, result.Error);
        else
            logger.LogInformation("Connected Haven's own container to the '{NetworkName}' control-plane network", DomainConstants.SystemNetworkName);
    }
}
