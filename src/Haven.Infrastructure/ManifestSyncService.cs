using Haven.Application.Common.Interfaces;
using Haven.Application.Configuration;
using Haven.Application.Mappers;
using Haven.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure;

public sealed class ManifestSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<ManifestSyncService> logger) : IHostedService
{
    private readonly string _basePath = "manifests";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<ManifestsOptions>>();
        if (!options.CurrentValue.AutoSyncEnabled)
        {
            logger.LogInformation("Auto-sync from manifests is disabled. Skipping synchronization.");
            return;
        }
        logger.LogInformation("Synchronizing database from manifests...");

        var serializer = scope.ServiceProvider.GetRequiredService<IManifestSerializer>();
        var context = scope.ServiceProvider.GetRequiredService<HavenDbContext>();

        var existingServices = await context.Services
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Token, cancellationToken);

        var projects = await serializer.ReadProjectsAsync(cancellationToken);

        await SyncProjectsAsync(context, projects.ToList(), cancellationToken);

        RestoreServiceTokens(context, existingServices);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Synchronized {Count} project(s) from manifests", projects.Count);

        // Write project manifests back to ensure sync
        var syncedProjects = await context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .ToListAsync(cancellationToken);

        foreach (var project in syncedProjects)
        {
            await serializer.WriteProjectAsync(project, cancellationToken);
        }

        // Load networks from manifests
        var networks = await ReadNetworksFromManifestsAsync(cancellationToken);
        await SyncNetworksAsync(context, networks, cancellationToken);
        if (networks.Count > 0)
        {
            logger.LogInformation("Synchronized {Count} network(s) from manifests", networks.Count);
        }

        // Write network manifests back to ensure sync
        var syncedNetworks = await context.Networks.ToListAsync(cancellationToken);
        var allProjects = await context.Projects
            .Include(p => p.Environments)
            .ToListAsync(cancellationToken);

        foreach (var network in syncedNetworks.Where(n => n.ProjectId.HasValue && n.EnvironmentId.HasValue))
        {
            var project = allProjects.FirstOrDefault(p => p.Id == network.ProjectId);
            var environment = project?.Environments.FirstOrDefault(e => e.Id == network.EnvironmentId);

            if (project != null && environment != null)
            {
                await serializer.WriteNetworkAsync(project, environment, network, cancellationToken);
            }
        }

        // Backwards compatibility: create missing project-environment networks
        await CreateMissingProjectEnvironmentNetworksAsync(serializer, context, cancellationToken);
    }

    private async Task SyncProjectsAsync(
        HavenDbContext context,
        List<Haven.Domain.Aggregates.Project> manifestProjects,
        CancellationToken cancellationToken)
    {
        var existingProjectIds = await context.Projects
            .AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var manifestProjectIds = manifestProjects.Select(p => p.Id).ToHashSet();

        // Delete projects not in manifests
        var projectIdsToDelete = existingProjectIds
            .Where(id => !manifestProjectIds.Contains(id))
            .ToList();

        if (projectIdsToDelete.Any())
        {
            await context.Projects
                .Where(p => projectIdsToDelete.Contains(p.Id))
                .ExecuteDeleteAsync(cancellationToken);

            logger.LogInformation("Deleted {Count} project(s) not found in manifests", projectIdsToDelete.Count);
        }

        // Get remaining project IDs for comparison
        var remainingProjectIds = await context.Projects
            .AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var addedCount = 0;
        var updatedCount = 0;

        foreach (var manifestProject in manifestProjects)
        {
            if (remainingProjectIds.Contains(manifestProject.Id))
            {
                // Project exists - delete and re-add to sync from manifest
                await context.Projects
                    .Where(p => p.Id == manifestProject.Id)
                    .ExecuteDeleteAsync(cancellationToken);
                context.Projects.Add(manifestProject);
                updatedCount++;
            }
            else
            {
                // New project
                context.Projects.Add(manifestProject);
                addedCount++;
            }
        }

        if (addedCount > 0 || updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            if (addedCount > 0)
                logger.LogInformation("Added {Count} new project(s) from manifests", addedCount);
            if (updatedCount > 0)
                logger.LogInformation("Updated {Count} project(s) from manifests", updatedCount);
        }
    }

    private async Task SyncNetworksAsync(
        HavenDbContext context,
        List<Haven.Domain.Aggregates.Network> manifestNetworks,
        CancellationToken cancellationToken)
    {
        var existingNetworkIds = await context.Networks
            .AsNoTracking()
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        var manifestNetworkIds = manifestNetworks.Select(n => n.Id).ToHashSet();

        // Delete networks not in manifests
        var networkIdsToDelete = existingNetworkIds
            .Where(id => !manifestNetworkIds.Contains(id))
            .ToList();

        if (networkIdsToDelete.Any())
        {
            await context.Networks
                .Where(n => networkIdsToDelete.Contains(n.Id))
                .ExecuteDeleteAsync(cancellationToken);

            logger.LogInformation("Deleted {Count} network(s) not found in manifests", networkIdsToDelete.Count);
        }

        // Get remaining network IDs for comparison
        var remainingNetworkIds = await context.Networks
            .AsNoTracking()
            .Select(n => n.Id)
            .ToListAsync(cancellationToken);

        var addedCount = 0;
        var updatedCount = 0;

        foreach (var manifestNetwork in manifestNetworks)
        {
            if (remainingNetworkIds.Contains(manifestNetwork.Id))
            {
                // Network exists - delete and re-add to sync from manifest
                await context.Networks
                    .Where(n => n.Id == manifestNetwork.Id)
                    .ExecuteDeleteAsync(cancellationToken);
                context.Networks.Add(manifestNetwork);
                updatedCount++;
            }
            else
            {
                // New network
                context.Networks.Add(manifestNetwork);
                addedCount++;
            }
        }

        if (addedCount > 0 || updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            if (addedCount > 0)
                logger.LogInformation("Added {Count} new network(s) from manifests", addedCount);
            if (updatedCount > 0)
                logger.LogInformation("Updated {Count} network(s) from manifests", updatedCount);
        }
    }

    private async Task<List<Haven.Domain.Aggregates.Network>> ReadNetworksFromManifestsAsync(CancellationToken cancellationToken)
    {
        var networks = new List<Haven.Domain.Aggregates.Network>();
        var projectsPath = Path.Combine(_basePath, "projects");

        if (!Directory.Exists(projectsPath))
            return networks;

        foreach (var projectDir in Directory.EnumerateDirectories(projectsPath))
        {
            var environmentsPath = Path.Combine(projectDir, "environments");
            if (!Directory.Exists(environmentsPath))
                continue;

            foreach (var environmentDir in Directory.EnumerateDirectories(environmentsPath))
            {
                var networkFilePath = Path.Combine(environmentDir, "network.yaml");

                if (File.Exists(networkFilePath))
                {
                    try
                    {
                        var yaml = await File.ReadAllTextAsync(networkFilePath, cancellationToken);
                        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                            .Build();

                        var manifest = deserializer.Deserialize<Haven.Application.Features.Networks.NetworkManifestDto>(yaml);

                        // Extract project and environment IDs from the environment manifest
                        var envManifestPath = Path.Combine(environmentDir, "environment.yaml");
                        var envYaml = await File.ReadAllTextAsync(envManifestPath, cancellationToken);
                        var envManifest = deserializer.Deserialize<Haven.Application.Features.Environments.EnvironmentManifestDto>(envYaml);

                        var network = manifest.FromManifest(envManifest.ProjectId, envManifest.Id);
                        networks.Add(network);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to read network manifest from {Path}", networkFilePath);
                    }
                }
            }
        }

        return networks;
    }

    private async Task CreateMissingProjectEnvironmentNetworksAsync(
        IManifestSerializer serializer,
        HavenDbContext context,
        CancellationToken cancellationToken)
    {
        var projectsWithoutNetworks = new List<(Haven.Domain.Aggregates.Project Project, Haven.Domain.Entities.Environment Environment)>();

        var projects = await context.Projects
            .Include(p => p.Environments)
            .ToListAsync(cancellationToken);

        var existingNetworks = await context.Networks
            .Where(n => n.Type == Haven.Domain.NetworkType.ProjectEnvironment)
            .ToListAsync(cancellationToken);

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
                var network = Haven.Domain.Aggregates.Network.CreateProjectEnvironmentNetwork(
                    project.Id,
                    project.Name,
                    environment.Id,
                    environment.Name);

                context.Networks.Add(network);
                await context.SaveChangesAsync(cancellationToken);

                // Save network manifest
                await serializer.WriteNetworkAsync(project, environment, network, cancellationToken);

                logger.LogInformation("Created missing network for project '{ProjectName}' environment '{EnvironmentName}'",
                    project.Name, environment.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create missing network for project '{ProjectName}' environment '{EnvironmentName}'",
                    project.Name, environment.Name);
            }
        }
    }

    private void RestoreServiceTokens(HavenDbContext context, Dictionary<Guid, string> existingServiceTokens)
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
