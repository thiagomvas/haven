using FastEndpoints;
using FastEndpoints.Swagger;
using Haven.Application;
using Haven.Infrastructure;
using Haven.Presentation.Api.Extensions;
using Haven.Presentation.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Haven.Presentation.Api");

    if (context.HostingEnvironment.IsDevelopment())
    {
        config.MinimumLevel.Debug();
    }
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFastEndpoints();
builder.Services.AddHangfireJobScheduling();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseHangfireJobScheduling();
app.UseFastEndpoints(config => config.Endpoints.RoutePrefix = "api");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();