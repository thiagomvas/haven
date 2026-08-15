using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ServiceStatus = Haven.Domain.Enums.ServiceStatus;

namespace Haven.Infrastructure.Deployment.Docker;

public class ContainerStateSyncService : IHostedService
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<ContainerStateSyncService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ContainerStateSyncService(
        IDockerClient dockerClient,
        ILogger<ContainerStateSyncService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _dockerClient = dockerClient;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting container state sync on application startup");

        try
        {
            await SyncContainerStatesAsync(cancellationToken);
            _logger.LogInformation("Container state sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during container state sync");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SyncContainerStatesAsync(CancellationToken cancellationToken)
    {
        var parameters = new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "label",
                    new Dictionary<string, bool>
                    {
                        { "haven.managed=true", true }
                    }
                }
            }
        };

        var containers = await _dockerClient.Containers.ListContainersAsync(parameters, cancellationToken);
        _logger.LogInformation("Found {ContainerCount} managed containers in Docker", containers.Count);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HavenDbContext>();

        var allProjects = await db.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .ToListAsync(cancellationToken);

        foreach (var project in allProjects)
        {
            foreach (var environment in project.Environments)
            {
                foreach (var service in environment.Services)
                {
                    var containerIdLabel = DockerUtils.BuildIdLabel(service.Id);
                    var matchingContainers = containers.Where(c =>
                        c.Labels != null &&
                        c.Labels.TryGetValue(containerIdLabel.Key, out var labelValue) &&
                        labelValue == containerIdLabel.Value
                    ).ToList();

                    if (matchingContainers.Count == 0)
                    {
                        if (service.Status != ServiceStatus.Stopped)
                        {
                            _logger.LogWarning(
                                "Service {ServiceName} in {EnvironmentName}/{ProjectName} is marked as {Status} but no container found. Marking as stopped.",
                                service.Name, environment.Name, project.Name, service.Status);
                            project.StopService(environment.Id, service.Id);
                        }
                    }
                    else
                    {
                        var container = matchingContainers.First();
                        var isRunning = container.State == "running";

                        if (isRunning && service.Status != ServiceStatus.Running)
                        {
                            _logger.LogWarning(
                                "Service {ServiceName} in {EnvironmentName}/{ProjectName} is marked as {Status} but container is running. Marking as running.",
                                service.Name, environment.Name, project.Name, service.Status);
                            project.DeployService(environment.Id, service.Id);
                        }
                        else if (!isRunning && service.Status == ServiceStatus.Running)
                        {
                            _logger.LogWarning(
                                "Service {ServiceName} in {EnvironmentName}/{ProjectName} is marked as running but container state is {ContainerState}. Marking as stopped.",
                                service.Name, environment.Name, project.Name, container.State);
                            project.StopService(environment.Id, service.Id);
                        }

                        if (matchingContainers.Count > 1)
                        {
                            _logger.LogWarning(
                                "Service {ServiceName} in {EnvironmentName}/{ProjectName} has {ContainerCount} matching containers. Cleaning up extras.",
                                service.Name, environment.Name, project.Name, matchingContainers.Count);

                            for (int i = 1; i < matchingContainers.Count; i++)
                            {
                                var extraContainer = matchingContainers[i];
                                try
                                {
                                    if (extraContainer.State == "running")
                                    {
                                        await _dockerClient.Containers.StopContainerAsync(extraContainer.ID,
                                            new ContainerStopParameters(), cancellationToken);
                                    }

                                    await _dockerClient.Containers.RemoveContainerAsync(extraContainer.ID,
                                        new ContainerRemoveParameters { Force = true }, cancellationToken);

                                    _logger.LogInformation(
                                        "Cleaned up duplicate container {ContainerId} for service {ServiceName}",
                                        extraContainer.ID, service.Name);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex,
                                        "Failed to clean up duplicate container {ContainerId} for service {ServiceName}",
                                        extraContainer.ID, service.Name);
                                }
                            }
                        }
                    }
                }
            }
        }

        var allSidecars = await db.Sidecars.ToListAsync(cancellationToken);

        foreach (var sidecar in allSidecars)
        {
            var containerIdLabel = DockerUtils.BuildIdLabel(sidecar.Id);
            var matchingContainers = containers.Where(c =>
                c.Labels != null &&
                c.Labels.TryGetValue(containerIdLabel.Key, out var labelValue) &&
                labelValue == containerIdLabel.Value
            ).ToList();

            if (matchingContainers.Count == 0)
            {
                if (sidecar.Status != ServiceStatus.Stopped)
                {
                    _logger.LogWarning(
                        "Sidecar {SidecarName} is marked as {Status} but no container found. Marking as stopped.",
                        sidecar.Name, sidecar.Status);
                    sidecar.MarkStopped();
                }
            }
            else
            {
                var container = matchingContainers.First();
                var isRunning = container.State == "running";

                if (isRunning && sidecar.Status != ServiceStatus.Running)
                {
                    _logger.LogWarning(
                        "Sidecar {SidecarName} is marked as {Status} but container is running. Marking as running.",
                        sidecar.Name, sidecar.Status);
                    sidecar.MarkDeployed();
                }
                else if (!isRunning && sidecar.Status == ServiceStatus.Running)
                {
                    _logger.LogWarning(
                        "Sidecar {SidecarName} is marked as running but container state is {ContainerState}. Marking as stopped.",
                        sidecar.Name, container.State);
                    sidecar.MarkStopped();
                }

                if (matchingContainers.Count > 1)
                {
                    _logger.LogWarning(
                        "Sidecar {SidecarName} has {ContainerCount} matching containers. Cleaning up extras.",
                        sidecar.Name, matchingContainers.Count);

                    for (int i = 1; i < matchingContainers.Count; i++)
                    {
                        var extraContainer = matchingContainers[i];
                        try
                        {
                            if (extraContainer.State == "running")
                            {
                                await _dockerClient.Containers.StopContainerAsync(extraContainer.ID,
                                    new ContainerStopParameters(), cancellationToken);
                            }

                            await _dockerClient.Containers.RemoveContainerAsync(extraContainer.ID,
                                new ContainerRemoveParameters { Force = true }, cancellationToken);

                            _logger.LogInformation(
                                "Cleaned up duplicate container {ContainerId} for sidecar {SidecarName}",
                                extraContainer.ID, sidecar.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Failed to clean up duplicate container {ContainerId} for sidecar {SidecarName}",
                                extraContainer.ID, sidecar.Name);
                        }
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}