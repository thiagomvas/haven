using System.Text.Json.Serialization;
using FastEndpoints;
using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;
using Haven.Presentation.Api.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Integration.Tests.Common;

public class IntegrationTestFixture : IDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    public HttpClient Client { get; private set; } = null!;
    public TestEventCollector EventCollector { get; private set; } = null!;
    public System.Text.Json.JsonSerializerOptions JsonSerializerOptions { get; private set; } = null!;
    private IServiceScope _scope = null!;
    private string _dbConnectionString = null!;

    public async Task InitializeAsync()
    {
        // Generate fresh connection string for each test
        _dbConnectionString = $"DataSource=file:memdb{Guid.NewGuid()}?mode=memory&cache=shared";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real DbContext
                    services.RemoveAll(typeof(DbContextOptions<HavenDbContext>));

                    // Add in-memory test database with unique connection string per test
                    services.AddDbContext<HavenDbContext>(opts =>
                        opts.UseSqlite(_dbConnectionString)
                    );

                    // Disable background services that need real infrastructure
                    services.RemoveAll(typeof(IHostedService));

                    // Replace manifest serializer with no-op implementation for tests
                    services.RemoveAll(typeof(IManifestSerializer));
                    services.AddSingleton<IManifestSerializer, NoOpManifestSerializer>();

                    // Replace manifest sync service with no-op implementation for tests
                    services.RemoveAll(typeof(IManifestSyncService));
                    services.AddSingleton<IManifestSyncService, NoOpManifestSyncService>();

                    // Configure JSON serialization for Optional types
                    services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
                    {
                        options.SerializerOptions.Converters.Add(new OptionalJsonConverterFactory());
                        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

                });
            });

        // Initialize database before creating the client
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HavenDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        Client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();

        // Configure JSON serializer options with Optional converter for client requests
        JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions();
        JsonSerializerOptions.Converters.Add(new OptionalJsonConverterFactory());
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Create event collector pointing to the test database
        var dbContext = _scope.ServiceProvider.GetRequiredService<HavenDbContext>();
        EventCollector = new TestEventCollector(dbContext);
    }

    public T GetService<T>() where T : notnull
        => _scope.ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        _scope?.Dispose();
        _factory?.Dispose();
    }
}

internal sealed class NoOpManifestSerializer : IManifestSerializer
{
    public Task WriteProjectAsync(Project project, CancellationToken ct) => Task.CompletedTask;
    public Task DeleteProjectAsync(Project project, CancellationToken ct) => Task.CompletedTask;
    public Task RenameProjectAsync(string oldProjectName, string newProjectName, CancellationToken ct) => Task.CompletedTask;
    public Task WriteEnvironmentAsync(Project project, Environment environment, CancellationToken ct) => Task.CompletedTask;
    public Task DeleteEnvironmentAsync(Project project, string environmentName, CancellationToken ct) => Task.CompletedTask;
    public Task RenameEnvironmentAsync(Project project, string oldEnvironmentName, string newEnvironmentName, CancellationToken ct) => Task.CompletedTask;
    public Task WriteServiceAsync(Project project, Environment environment, Service service, CancellationToken ct) => Task.CompletedTask;
    public Task DeleteServiceAsync(Project project, Environment environment, string serviceName, CancellationToken ct) => Task.CompletedTask;
    public Task RenameServiceAsync(Project project, Environment environment, string oldServiceName, string newServiceName, CancellationToken ct) => Task.CompletedTask;
    public Task WriteNetworkAsync(Project project, Environment environment, Network network, CancellationToken ct) => Task.CompletedTask;
    public Task DeleteNetworkAsync(Project project, Environment environment, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class NoOpManifestSyncService : IManifestSyncService
{
    public Task SyncAsync(CancellationToken ct = default) => Task.CompletedTask;
}
