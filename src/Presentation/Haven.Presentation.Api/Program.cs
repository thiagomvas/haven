using System.Text;
using System.Text.Json.Serialization;

using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;

using Hangfire;

using Haven.Application;
using Haven.Application.Common.Interfaces;
using Haven.Infrastructure;
using Haven.Infrastructure.Extensions;
using Haven.Infrastructure.Persistence;
using Haven.Presentation.Api;
using Haven.Presentation.Api.Cors;
using Haven.Presentation.Api.Extensions;
using Haven.Presentation.Api.Middleware;
using Haven.Presentation.Api.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Scalar.AspNetCore;

using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

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

var telemetryOptions = Haven.Infrastructure.Configuration.TelemetryStartupReader.Read(
    builder.Configuration.GetConnectionString("DefaultConnection"));

if (telemetryOptions.Enabled)
{
    var serviceName = string.IsNullOrWhiteSpace(telemetryOptions.ServiceName)
        ? "Haven"
        : telemetryOptions.ServiceName;

    var otlpProtocol = telemetryOptions.Protocol == Haven.Application.Configuration.OtlpProtocol.Grpc
        ? OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
        : OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(serviceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                o.Protocol = otlpProtocol;
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(Haven.Application.Common.Telemetry.HavenMetrics.MeterName)
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                o.Protocol = otlpProtocol;
            }));
}

builder.Host.UseSerilog((context, config) =>
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

var app = builder.Build();

app.UseConfiguredHangfireServer();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ValidateSetupMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
    config.Serializer.Options.Converters.Add(new OptionalJsonConverterFactory());
    config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
    config.Serializer.Options.Converters.Add(app.Services.GetRequiredService<TimezoneAwareDateTimeOffsetConverter>());
    config.Serializer.Options.Converters.Add(app.Services.GetRequiredService<TimezoneAwareDateTimeConverter>());
    config.Serializer.Options.PropertyNameCaseInsensitive = true;
});

if (app.Environment.IsDevelopment())
{
    app.MapHangfireDashboard("/hangfire");
}

app.UseSwaggerGen(options =>
{
    options.Path = "/openapi/{documentName}.json";
});
app.MapScalarApiReference(opt =>
{
    if (app.Environment.IsDevelopment()) opt.EnablePersistentAuthentication();
});
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HavenDbContext>();
    context.Database.EnsureCreated();
    context.Database.Migrate();

    var seedService = scope.ServiceProvider
        .GetRequiredService<Haven.Application.Common.Interfaces.IHavenConfigurationSeedService>();
    await seedService.SeedAsync(CancellationToken.None);
    await context.SaveChangesAsync(CancellationToken.None);

    var optionsMonitor = app.Services
        .GetRequiredService<
            Microsoft.Extensions.Options.IOptionsMonitor<Haven.Application.Configuration.ManifestsOptions>>();
    Haven.Infrastructure.Utils.PathResolver.Initialize(optionsMonitor);
    
    var scheduler = scope.ServiceProvider.GetRequiredService<IConfigurationWriteScheduler>();
    scheduler.ScheduleWrite();
}

app.MapHavenHubs();
app.MapFallbackToFile("index.html");
app.Run();