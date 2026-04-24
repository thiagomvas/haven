using System.Text.Json.Serialization;
using FastEndpoints;
using Haven.Infrastructure.Persistence;
using Haven.Presentation.Api.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Haven.Integration.Tests.Common;

public class IntegrationTestFixture : IDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    public HttpClient Client { get; private set; } = null!;
    public TestEventCollector EventCollector { get; private set; } = null!;
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
