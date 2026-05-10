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

        await context.Projects.ExecuteDeleteAsync(cancellationToken);
        await context.Networks.ExecuteDeleteAsync(cancellationToken);
        context.Projects.AddRange(projects);
        await context.SaveChangesAsync(cancellationToken);

        RestoreServiceTokens(context, existingServices);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Synchronized {Count} project(s) from manifests", projects.Count);

        // Load networks from manifests
        var networks = await ReadNetworksFromManifestsAsync(cancellationToken);
        if (networks.Count > 0)
        {
            context.Networks.AddRange(networks);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Synchronized {Count} network(s) from manifests", networks.Count);
        }

        // Backwards compatibility: create missing project-environment networks
        await CreateMissingProjectEnvironmentNetworksAsync(serializer, context, cancellationToken);
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
