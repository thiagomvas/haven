using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure;

public sealed class ManifestSyncOrchestrator(
    IManifestSerializer<Project> projectSerializer,
    IManifestSerializer<Network> networkSerializer,
    HavenDbContext context,
    ILogger<ManifestSyncOrchestrator> logger) : IManifestSyncService
{
    public async Task SyncAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Synchronizing database from manifests...");

        await SyncProjectsAsync(ct);
        await SyncNetworksAsync(ct);
        await CreateMissingProjectEnvironmentNetworksAsync(ct);

        logger.LogInformation("Manifest synchronization completed successfully");
    }

    private async Task SyncProjectsAsync(CancellationToken ct)
    {
        var existingServices = await context.Services
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Token, ct);

        var manifestProjects = await projectSerializer.ReadAsync(ct: ct);

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var existingCount = await context.Projects.CountAsync(ct);
            if (existingCount > 0)
            {
                await context.Projects.ExecuteDeleteAsync(ct);
                logger.LogInformation("Deleted all {Count} existing project(s)", existingCount);
            }

            if (manifestProjects.Count > 0)
            {
                context.Projects.AddRange(manifestProjects);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Added {Count} project(s) from manifests", manifestProjects.Count);
            }

            RestoreServiceTokens(existingServices);
            await context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var syncedProjects = await context.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .ToListAsync(ct);

        foreach (var project in syncedProjects)
        {
            await projectSerializer.WriteAsync(project, ct);
        }
    }

    private async Task SyncNetworksAsync(CancellationToken ct)
    {
        var manifestNetworks = await networkSerializer.ReadAsync(ct: ct);

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var existingCount = await context.Networks.CountAsync(ct);
            if (existingCount > 0)
            {
                await context.Networks.ExecuteDeleteAsync(ct);
                logger.LogInformation("Deleted all {Count} existing network(s)", existingCount);
            }

            if (manifestNetworks.Count > 0)
            {
                context.Networks.AddRange(manifestNetworks);
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Added {Count} network(s) from manifests", manifestNetworks.Count);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var syncedNetworks = await context.Networks
            .Where(n => n.Type == Domain.NetworkType.ProjectEnvironment)
            .Include(n => n.Project)
            .Include(n => n.Environment)
            .ToListAsync(ct);

        foreach (var network in syncedNetworks)
        {
            if (network.Project != null && network.Environment != null)
            {
                await networkSerializer.WriteAsync(network, ct);
            }
        }
    }

    private async Task CreateMissingProjectEnvironmentNetworksAsync(CancellationToken ct)
    {
        var projectsWithoutNetworks = new List<(Project Project, Environment Environment)>();

        var projects = await context.Projects
            .Include(p => p.Environments)
            .ToListAsync(ct);

        var existingNetworks = await context.Networks
            .Where(n => n.Type == Domain.NetworkType.ProjectEnvironment)
            .ToListAsync(ct);

        foreach (var project in projects)
        {
            foreach (var environment in project.Environments)
            {
                var hasNetwork = existingNetworks.Any(n =>
                    n.ProjectId == project.Id &&
                    n.EnvironmentId == environment.Id);

                if (!hasNetwork)
                {
                    projectsWithoutNetworks.Add((project, environment));
                }
            }
        }

        if (projectsWithoutNetworks.Count == 0)
            return;

        logger.LogInformation("Creating {Count} missing project-environment network(s) for backwards compatibility", projectsWithoutNetworks.Count);

        foreach (var (project, environment) in projectsWithoutNetworks)
        {
            try
            {
                var network = Domain.Aggregates.Network.CreateProjectEnvironmentNetwork(
                    project.Id,
                    project.Name,
                    environment.Id,
                    environment.Name);

                context.Networks.Add(network);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create missing network for project '{ProjectName}' environment '{EnvironmentName}'",
                    project.Name, environment.Name);
            }
        }

        await context.SaveChangesAsync(ct);

        var createdNetworks = await context.Networks
            .Where(n => n.Type == Domain.NetworkType.ProjectEnvironment)
            .Include(n => n.Project)
            .Include(n => n.Environment)
            .Where(n => projectsWithoutNetworks.Any(p => p.Project.Id == n.ProjectId && p.Environment.Id == n.EnvironmentId))
            .ToListAsync(ct);

        foreach (var network in createdNetworks)
        {
            try
            {
                await networkSerializer.WriteAsync(network, ct);

                if (network.Project != null && network.Environment != null)
                {
                    logger.LogInformation("Created missing network for project '{ProjectName}' environment '{EnvironmentName}'",
                        network.Project.Name, network.Environment.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write network manifest for project '{ProjectId}' environment '{EnvironmentId}'",
                    network.ProjectId, network.EnvironmentId);
            }
        }
    }

    private void RestoreServiceTokens(Dictionary<Guid, string> existingServiceTokens)
    {
        var allServices = context.Services.Local.ToList();
        foreach (var service in allServices)
        {
            if (existingServiceTokens.TryGetValue(service.Id, out var token))
            {
                service.Token = token;
            }
        }
    }
}