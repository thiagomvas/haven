using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Haven.Application;
using Haven.Infrastructure;
using Haven.Infrastructure.Extensions;
using Haven.Infrastructure.Persistence;
using Haven.Presentation.Api;
using Haven.Presentation.Api.Extensions;
using Haven.Presentation.Api.Middleware;
using Haven.Presentation.Api.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
    options.ListenAnyIP(8443, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Haven.Presentation.Api");

    config.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:8080",
                "http://localhost:8443")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddApplication();
builder.Services.AddPresentation();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.AutoTagPathSegmentIndex = 0;
        o.ShortSchemaNames = true;
    });

var app = builder.Build();

app.UseConfiguredHangfireServer();

app.UseMiddleware<GlobalExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseStaticFiles();
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
    config.Serializer.Options.Converters.Add(new OptionalJsonConverterFactory());
    config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen(options =>
    {
        options.Path = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HavenDbContext>();
    context.Database.EnsureCreated();
    context.Database.Migrate();

    // Load Haven configuration from YAML file first, before anything else runs
    var configSerializer = scope.ServiceProvider.GetRequiredService<Haven.Application.Common.Interfaces.IHavenConfigurationSerializer>();
    var configRepository = scope.ServiceProvider.GetRequiredService<Haven.Application.Common.Interfaces.Repositories.IHavenSettingRepository>();
    var config = await configSerializer.ReadAsync(CancellationToken.None);
    var manifestsJson = System.Text.Json.JsonSerializer.Serialize(config.Manifests);
    await configRepository.UpsertAsync(Haven.Application.Configuration.ManifestsOptions.SectionName, manifestsJson, CancellationToken.None);
    await context.SaveChangesAsync(CancellationToken.None);

    // Initialize PathResolver with the options monitor so it respects the manifest path configuration
    var optionsMonitor = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Haven.Application.Configuration.ManifestsOptions>>();
    Haven.Infrastructure.Utils.PathResolver.Initialize(optionsMonitor);
}

app.MapHavenHubs();
app.MapFallbackToFile("index.html");
app.Run();