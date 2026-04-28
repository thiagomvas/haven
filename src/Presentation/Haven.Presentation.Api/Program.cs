using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Haven.Application;
using Haven.Infrastructure;
using Haven.Infrastructure.Persistence;
using Haven.Presentation.Api.Extensions;
using Haven.Presentation.Api.Middleware;
using Haven.Presentation.Api.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

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
        .MinimumLevel.Information()
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Haven.Presentation.Api");
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
            .AllowAnyHeader();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.AutoTagPathSegmentIndex = 0;
        o.ShortSchemaNames = true;
    });
builder.Services.AddHangfireJobScheduling();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();
app.UseHangfireJobScheduling();
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
}

app.MapFallbackToFile("index.html");

app.Run();