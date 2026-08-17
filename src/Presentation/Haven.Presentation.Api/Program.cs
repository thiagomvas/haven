using System.Text.Json.Serialization;

using FastEndpoints;
using FastEndpoints.Swagger;

using Hangfire;

using Haven.Infrastructure.Extensions;
using Haven.Presentation.Api;
using Haven.Presentation.Api.Bootstrapping;
using Haven.Presentation.Api.Middleware;
using Haven.Presentation.Api.Serialization;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var telemetryOptions = builder.ConfigureHavenServices();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseConfiguredHangfireServer();
}
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

await app.RunHavenStartupTasksAsync();

if (telemetryOptions.Enabled)
{
    app.MapPrometheusScrapingEndpoint();
}

app.MapHavenHubs();
app.MapFallbackToFile("index.html");
app.Run();