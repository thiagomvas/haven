using Haven.Application.Common.Interfaces;
using Haven.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure;

public sealed class ManifestSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<ManifestSyncService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Synchronizing database from manifests...");

        using var scope = scopeFactory.CreateScope();
        var serializer = scope.ServiceProvider.GetRequiredService<IManifestSerializer>();
        var context = scope.ServiceProvider.GetRequiredService<HavenDbContext>();

        var projects = await serializer.ReadProjectsAsync(cancellationToken);

        await context.Projects.ExecuteDeleteAsync(cancellationToken);
        context.Projects.AddRange(projects);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Synchronized {Count} project(s) from manifests", projects.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
