using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Environment = System.Environment;
using Network = Haven.Domain.Aggregates.Network;

namespace Haven.Infrastructure.Deployment;

public class DockerNetworkingService : INetworkingService
{
    public ServiceType ServiceType => ServiceType.DockerImage;

    private readonly HavenDbContext _dbContext;
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<DockerNetworkingService> _logger;

    public DockerNetworkingService(HavenDbContext dbContext, ILogger<DockerNetworkingService> logger,
        IDockerClient dockerClient)
    {
        _dbContext = dbContext;
        _logger = logger;
        _dockerClient = dockerClient;
    }

    public async Task<Result> CreateProjectEnvironmentNetworkAsync(Guid projectId, Guid environmentId,
        CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project == null) return Error.NotFoundFor(nameof(Project), projectId);

        var environment =
            await _dbContext.Environments.FirstOrDefaultAsync(e => e.Id == environmentId,
                cancellationToken: cancellationToken);
        if (environment == null) return Error.NotFoundFor(nameof(Environment), environmentId);

        var network = Network.CreateProjectEnvironmentNetwork(projectId, project.Name, environmentId, environment.Name);
        var networkName = DockerUtils.SanitizeForDocker(network.Name);

        var existingNetworks = await _dockerClient.Networks.ListNetworksAsync(
            new NetworksListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "name", new Dictionary<string, bool> { { networkName, true } } }
                }
            },
            cancellationToken);

        if (existingNetworks.Count > 0)
        {
            _logger.LogInformation("Docker network '{NetworkName}' already exists, skipping creation.", networkName);
            return Result.Success();
        }

        try
        {
            var createResponse = await _dockerClient.Networks.CreateNetworkAsync(
                new NetworksCreateParameters
                {
                    Name = networkName,
                    Driver = "bridge",
                    Attachable = true,
                    CheckDuplicate = true,
                    Labels = new Dictionary<string, string>
                    {
                        { "haven.project-id", projectId.ToString() },
                        { "haven.environment-id", environmentId.ToString() },
                        { "haven.project-name", project.Name },
                        { "haven.environment-name", environment.Name },
                        { "haven.network-type", "environment" },
                        { "haven.network-id", network.Id.ToString() },
                        { "haven.created-at", DateTime.UtcNow.ToString("O") },
                        { DockerUtils.HavenManagedLabel.Key, DockerUtils.HavenManagedLabel.Value }
                    }
                },
                cancellationToken);


            _logger.LogInformation(
                "Successfully created Docker network {NetworkId} for project {ProjectId} environment {EnvironmentId}",
                createResponse, projectId, environmentId);

            network.SetDockerNetworkId(createResponse.ID);

            _dbContext.Networks.Add(network);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Network aggregate persisted for project {ProjectId} environment {EnvironmentId}",
                projectId, environmentId);

            return Result.Success();
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(
                ex,
                "Docker API error while creating network {NetworkName} for project {ProjectId} environment {EnvironmentId}: {ErrorMessage}",
                networkName, projectId, environmentId, ex.Message);

            return Error.Failure(
                "Docker.NetworkException",
                $"Failed to create Docker network: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating network for project {ProjectId} environment {EnvironmentId}",
                projectId, environmentId);

            return Error.Failure(
                "Docker.Unexpected",
                "An unexpected error occurred while creating the network");
        }
    }

    public async Task<Result> ConnectServiceToNetworksAsync(Guid serviceId, IEnumerable<Guid> networkIds,
        CancellationToken cancellationToken)
    {
        var service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service == null) return Error.NotFoundFor(nameof(Service), serviceId);

        var networkIdsList = networkIds.ToList();
        var networks = await _dbContext.Networks
            .Where(n => networkIdsList.Contains(n.Id))
            .ToListAsync(cancellationToken);

        if (networks.Count == 0)
            return Error.NotFoundFor(nameof(Network), networkIdsList.FirstOrDefault());

        var containerId = await GetServiceContainerIdAsync(service, cancellationToken);
        if (containerId == null)
        {
            _logger.LogWarning(
                "Cannot connect service {ServiceId} to networks because no running container was found.",
                serviceId);
            return Error.Failure("Docker.ContainerNotFound", "No running container found for the service.");
        }

        var errors = new List<string>();
        foreach (var network in networks)
        {
            var ensureResult = await EnsureNetworkExistsAsync(network, cancellationToken);
            if (ensureResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to ensure network {NetworkId} exists for service {ServiceId}",
                    network.Id,
                    serviceId);
                errors.Add($"Failed to ensure network {network.Name} exists");
                continue;
            }

            if (string.IsNullOrEmpty(network.DockerNetworkId))
            {
                _logger.LogWarning(
                    "Network {NetworkId} has no Docker network ID after ensure check, skipping connection for service {ServiceId}",
                    network.Id,
                    serviceId);
                continue;
            }

            try
            {
                await _dockerClient.Networks.ConnectNetworkAsync(
                    network.DockerNetworkId,
                    new NetworkConnectParameters
                    {
                        Container = containerId,
                        EndpointConfig = new EndpointSettings()
                    },
                    cancellationToken);

                var existingConnection = await _dbContext.ServiceNetworks
                    .FirstOrDefaultAsync(sn => sn.ServiceId == serviceId && sn.NetworkId == network.Id, cancellationToken);

                if (existingConnection == null)
                {
                    _dbContext.ServiceNetworks.Add(ServiceNetwork.Create(serviceId, network.Id));
                }

                _logger.LogInformation(
                    "Connected service {ServiceId} (container {ContainerId}) to network {NetworkId}",
                    serviceId, containerId, network.Id);
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug(
                    "Network {NetworkId} not found when connecting service {ServiceId}",
                    network.DockerNetworkId,
                    serviceId);
            }
            catch (DockerApiException ex)
            {
                var errorMsg = $"Failed to connect to network {network.Name}: {ex.Message}";
                errors.Add(errorMsg);

                _logger.LogWarning(
                    ex,
                    "Docker API error connecting service {ServiceId} to network {NetworkId}: {ErrorMessage}",
                    serviceId,
                    network.DockerNetworkId,
                    ex.Message);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} connected to {NetworkCount} networks",
            serviceId,
            networks.Count);

        if (errors.Any())
        {
            _logger.LogWarning(
                "Connection completed with {ErrorCount} Docker API errors for service {ServiceId}",
                errors.Count,
                serviceId);
        }

        return Result.Success();
    }

    public async Task<Result> DisconnectServiceFromNetworksAsync(Guid serviceId, IEnumerable<Guid> networkIds,
        CancellationToken cancellationToken)
    {
        var service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service == null) return Error.NotFoundFor(nameof(Service), serviceId);

        var networkIdsList = networkIds.ToList();
        var serviceNetworks = await _dbContext.ServiceNetworks
            .AsNoTracking()
            .Include(sn => sn.Network)
            .Where(sn => sn.ServiceId == serviceId && networkIdsList.Contains(sn.NetworkId))
            .ToListAsync(cancellationToken);

        if (serviceNetworks.Count == 0)
        {
            _logger.LogWarning(
                "No network connections found for service {ServiceId} when attempting to disconnect from specified networks.",
                serviceId);
            return Result.Success();
        }

        var containerId = await GetServiceContainerIdAsync(service, cancellationToken);
        if (containerId == null)
        {
            _logger.LogWarning(
                "Cannot disconnect service {ServiceId} from networks because no running container was found.",
                serviceId);
            return Error.Failure("Docker.ContainerNotFound", "No running container found for the service.");
        }

        var errors = new List<string>();
        foreach (var serviceNetwork in serviceNetworks)
        {
            try
            {
                await _dockerClient.Networks.DisconnectNetworkAsync(
                    serviceNetwork.Network!.DockerNetworkId,
                    new NetworkDisconnectParameters
                    {
                        Container = containerId,
                        Force = true
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Disconnected service {ServiceId} (container {ContainerId}) from network {NetworkId}",
                    serviceId, containerId, serviceNetwork.NetworkId);
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug(
                    "Network {NetworkId} not found when disconnecting service {ServiceId}; may have been deleted",
                    serviceNetwork.Network!.DockerNetworkId,
                    serviceId);
            }
            catch (DockerApiException ex)
            {
                var errorMsg = $"Failed to disconnect from network {serviceNetwork.Network!.Name}: {ex.Message}";
                errors.Add(errorMsg);

                _logger.LogWarning(
                    ex,
                    "Docker API error disconnecting service {ServiceId} from network {NetworkId}: {ErrorMessage}",
                    serviceId,
                    serviceNetwork.Network.DockerNetworkId,
                    ex.Message);
            }
        }

        _dbContext.ServiceNetworks.RemoveRange(serviceNetworks);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} disconnected from {NetworkCount} networks",
            serviceId,
            serviceNetworks.Count);

        if (errors.Any())
        {
            _logger.LogWarning(
                "Disconnection completed with {ErrorCount} Docker API errors for service {ServiceId}",
                errors.Count,
                serviceId);
        }

        return Result.Success();
    }

    public async Task<Result> DisconnectServiceFromAllNetworksAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var serviceNetworks = await _dbContext.ServiceNetworks
            .AsNoTracking()
            .Include(sn => sn.Network)
            .Where(sn => sn.ServiceId == serviceId)
            .ToListAsync(cancellationToken);

        if (serviceNetworks.Count <= 0)
        {
            _logger.LogWarning(
                "No network connections found for service {ServiceId} when attempting to disconnect from all networks.",
                serviceId);
            return Result.Success();
        }

        var service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service == null) return Error.NotFoundFor(nameof(Service), serviceId);

        var containerId = await GetServiceContainerIdAsync(service, cancellationToken);
        if (containerId == null)
        {
            _logger.LogWarning(
                "Cannot disconnect service {ServiceId} from networks because no running container was found.",
                serviceId);
            return Error.Failure("Docker.ContainerNotFound", "No running container found for the service.");
        }

        var errors = new List<string>();
        foreach (var network in serviceNetworks)
        {
            try
            {
                await _dockerClient.Networks.DisconnectNetworkAsync(
                    network.Network!.DockerNetworkId,
                    new NetworkDisconnectParameters
                    {
                        Container = containerId,
                        Force = true
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Disconnected service {ServiceId} (container {ContainerId}) from network {NetworkId}",
                    serviceId, containerId, network.NetworkId);
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug(
                    "Network {NetworkId} not found when disconnecting service {ServiceId}; may have been deleted",
                    network.Network!.DockerNetworkId,
                    serviceId);
            }
            catch (DockerApiException ex)
            {
                var errorMsg = $"Failed to disconnect from network {network.Network!.Name}: {ex.Message}";
                errors.Add(errorMsg);

                _logger.LogWarning(
                    ex,
                    "Docker API error disconnecting service {ServiceId} from network {NetworkId}: {ErrorMessage}",
                    serviceId,
                    network.Network.DockerNetworkId,
                    ex.Message);
            }
        }

        _dbContext.ServiceNetworks.RemoveRange(serviceNetworks);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} disconnected from {NetworkCount} networks",
            serviceId,
            serviceNetworks.Count);

        if (errors.Any())
        {
            _logger.LogWarning(
                "Disconnection completed with {ErrorCount} Docker API errors for service {ServiceId}",
                errors.Count,
                serviceId);
        }

        return Result.Success();
    }

    private async Task<Result> EnsureNetworkExistsAsync(Network network, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(network.DockerNetworkId))
            return Result.Success();

        var networkName = DockerUtils.SanitizeForDocker(network.Name);

        try
        {
            var existingNetworks = await _dockerClient.Networks.ListNetworksAsync(
                new NetworksListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        { "name", new Dictionary<string, bool> { { networkName, true } } }
                    }
                },
                cancellationToken);

            if (existingNetworks.Count > 0)
            {
                _logger.LogInformation(
                    "Found existing Docker network '{NetworkName}' for network {NetworkId}, updating Docker network ID",
                    networkName, network.Id);
                network.SetDockerNetworkId(existingNetworks[0].ID);
                _dbContext.Networks.Update(network);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            if (network.Type == NetworkType.ProjectEnvironment && network.ProjectId.HasValue && network.EnvironmentId.HasValue)
            {
                _logger.LogInformation(
                    "Network '{NetworkName}' does not exist in Docker, creating it for network {NetworkId}",
                    networkName, network.Id);

                var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == network.ProjectId, cancellationToken);
                var environment = await _dbContext.Environments.FirstOrDefaultAsync(e => e.Id == network.EnvironmentId, cancellationToken);

                if (project == null || environment == null)
                {
                    return Error.NotFoundFor(project == null ? nameof(Project) : nameof(Environment),
                        network.ProjectId ?? network.EnvironmentId ?? Guid.Empty);
                }

                var createResponse = await _dockerClient.Networks.CreateNetworkAsync(
                    new NetworksCreateParameters
                    {
                        Name = networkName,
                        Driver = "bridge",
                        Attachable = true,
                        CheckDuplicate = true,
                        Labels = new Dictionary<string, string>
                        {
                            { "haven.project-id", network.ProjectId.ToString()! },
                            { "haven.environment-id", network.EnvironmentId.ToString()! },
                            { "haven.project-name", project.Name },
                            { "haven.environment-name", environment.Name },
                            { "haven.network-type", "environment" },
                            { "haven.network-id", network.Id.ToString() },
                            { "haven.created-at", DateTime.UtcNow.ToString("O") },
                            { DockerUtils.HavenManagedLabel.Key, DockerUtils.HavenManagedLabel.Value }
                        }
                    },
                    cancellationToken);

                network.SetDockerNetworkId(createResponse.ID);
                _dbContext.Networks.Update(network);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully created Docker network {NetworkId} for network {NetworkId}",
                    createResponse.ID, network.Id);

                return Result.Success();
            }

            return Error.Failure("Network.NotFound", $"Network '{networkName}' does not exist and cannot be auto-created");
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(
                ex,
                "Docker API error while ensuring network {NetworkName} (ID: {NetworkId}) exists: {ErrorMessage}",
                networkName, network.Id, ex.Message);
            return Error.Failure("Docker.NetworkException", $"Failed to ensure network exists: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error ensuring network {NetworkName} (ID: {NetworkId}) exists",
                networkName, network.Id);
            return Error.Failure("Docker.Unexpected", "An unexpected error occurred while ensuring the network exists");
        }
    }

    private async Task<string?> GetServiceContainerIdAsync(
        Service service,
        CancellationToken cancellationToken)
    {
        try
        {
            // Query containers matching the service's Docker container name
            var label = DockerUtils.BuildIdLabel(service.Id);

            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = false, // Only running containers
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        {
                            "label", new Dictionary<string, bool>()
                            {
                                { label.Key + "=" + label.Value, true }
                            }
                        }
                    }
                },
                cancellationToken);

            if (containers.Count == 0)
            {
                _logger.LogDebug(
                    "No running container found for service {ServiceId}",
                    service.Id);
                return null;
            }

            var containerId = containers.First().ID;
            _logger.LogDebug(
                "Found container {ContainerId} for service {ServiceId}",
                containerId,
                service.Id);

            return containerId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error retrieving container ID for service {ServiceId}",
                service.Id);
            return null;
        }
    }
}