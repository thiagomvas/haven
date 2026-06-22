using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

using FastEndpoints;

using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;
using Haven.Presentation.Api.Serialization;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

                    // Replace generic manifest serializers with no-op implementation for tests
                    services.RemoveAll(typeof(IManifestSerializer<Project>));
                    services.RemoveAll(typeof(IManifestSerializer<Environment>));
                    services.RemoveAll(typeof(IManifestSerializer<Service>));
                    services.RemoveAll(typeof(IManifestSerializer<Haven.Domain.Aggregates.Network>));
                    services.AddSingleton<IManifestSerializer<Project>, NoOpManifestSerializer<Project>>();
                    services.AddSingleton<IManifestSerializer<Environment>, NoOpManifestSerializer<Environment>>();
                    services.AddSingleton<IManifestSerializer<Service>, NoOpManifestSerializer<Service>>();
                    services
                        .AddSingleton<IManifestSerializer<Haven.Domain.Aggregates.Network>,
                            NoOpManifestSerializer<Haven.Domain.Aggregates.Network>>();

                    // Replace manifest sync service with no-op implementation for tests
                    services.RemoveAll(typeof(IManifestSyncService));
                    services.AddSingleton<IManifestSyncService, NoOpManifestSyncService>();

                    // Stub out setup check so ValidateSetupMiddleware doesn't redirect all requests
                    services.RemoveAll(typeof(IHavenService));
                    services.AddSingleton<IHavenService, NoOpHavenService>();

                    // Use background context so PermissionBehavior skips all permission checks
                    services.RemoveAll(typeof(ICurrentUserService));
                    services.AddSingleton<ICurrentUserService, TestCurrentUserService>();

                    // Configure JSON serialization for Optional types
                    services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
                    {
                        options.SerializerOptions.Converters.Add(new OptionalJsonConverterFactory());
                        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });
                    services.AddFastEndpoints();

                    // Replace JWT with a test auth scheme that always authenticates
                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
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

    public Task RenameProjectAsync(string oldProjectName, string newProjectName, CancellationToken ct) =>
        Task.CompletedTask;

    public Task WriteEnvironmentAsync(Project project, Environment environment, CancellationToken ct) =>
        Task.CompletedTask;

    public Task DeleteEnvironmentAsync(Project project, string environmentName, CancellationToken ct) =>
        Task.CompletedTask;

    public Task RenameEnvironmentAsync(Project project, string oldEnvironmentName, string newEnvironmentName,
        CancellationToken ct) => Task.CompletedTask;

    public Task WriteServiceAsync(Project project, Environment environment, Service service, CancellationToken ct) =>
        Task.CompletedTask;

    public Task DeleteServiceAsync(Project project, Environment environment, string serviceName,
        CancellationToken ct) => Task.CompletedTask;

    public Task RenameServiceAsync(Project project, Environment environment, string oldServiceName,
        string newServiceName, CancellationToken ct) => Task.CompletedTask;

    public Task WriteNetworkAsync(Project project, Environment environment, Network network, CancellationToken ct) =>
        Task.CompletedTask;

    public Task DeleteNetworkAsync(Project project, Environment environment, CancellationToken ct) =>
        Task.CompletedTask;
}

internal sealed class NoOpManifestSyncService : IManifestSyncService
{
    public Task SyncAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public bool IsAdmin => true;
    public bool IsBackgroundContext => true;
}

internal sealed class NoOpHavenService : IHavenService
{
    public Task<bool> RequiresFirstTimeSetupAsync(CancellationToken ct) => Task.FromResult(false);
    public Task<SetupStage> GetSetupStageAsync(CancellationToken ct) => Task.FromResult(SetupStage.Completed);
    public Task AdvanceSetupStageAsync(SetupStage stage, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class NoOpManifestSerializer<T> : IManifestSerializer<T> where T : class
{
    public Task WriteAsync(T item, CancellationToken ct = default) => Task.CompletedTask;
    public Task WriteToAsync(T item, string basePath, CancellationToken ct = default) => Task.CompletedTask;

    public Task RenameAsync(T item, string oldName, string newName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<T>> ReadAsync(Guid parentId = default, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<T>>([]);

    public Task RemoveAsync(T item, CancellationToken ct = default) => Task.CompletedTask;

    public Task<string> ReadManifestAsync(T item, CancellationToken ct = default) =>
        Task.FromResult<string>(string.Empty);

    public Type EntityType => typeof(T);
    public Task WriteToAsync(object item, string basePath, CancellationToken ct = default) => Task.CompletedTask;
}