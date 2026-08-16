using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Environment = System.Environment;
using Network = Haven.Domain.Aggregates.Network;

namespace Haven.Infrastructure.Deployment.Docker;

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

        // Docker's "name" list filter does a substring match, not an exact match, so re-filter for
        // an exact name before trusting a result.
        var existingNetworks = await _dockerClient.Networks.ListNetworksAsync(
            new NetworksListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    { "name", new Dictionary<string, bool> { { networkName, true } } }
                }
            },
            cancellationToken);

        if (existingNetworks.Any(n => n.Name == networkName))
        {
            _logger.LogInformation("Docker network '{NetworkName}' already exists, skipping creation.", networkName);
            return Result.Success();
        }

        try
        {
            var ipam = await TryBuildIpamAsync(projectId, environmentId, cancellationToken);

            var createResponse = await _dockerClient.Networks.CreateNetworkAsync(
                new NetworksCreateParameters
                {
                    Name = networkName,
                    Driver = "bridge",
                    Attachable = true,
                    CheckDuplicate = true,
                    IPAM = ipam is null
                        ? null
                        : new IPAM
                        {
                            Driver = "default",
                            Config = [new IPAMConfig { Subnet = ipam.Value.Subnet, Gateway = ipam.Value.Gateway }]
                        },
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
            if (ipam is not null)
                network.AssignNetworkInfo(ipam.Value.Subnet, ipam.Value.Gateway);

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

            return Error.Docker.FailedToCreateNetwork;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error creating network for project {ProjectId} environment {EnvironmentId}",
                projectId, environmentId);

            return Error.Docker.FailedToCreateNetwork;
        }
    }

    public async Task<Result> ConnectServiceToNetworksAsync(Guid serviceId, IEnumerable<Guid> networkIds,
        CancellationToken cancellationToken)
    {
        var networkIdsList = networkIds.ToList();
        var networks = await _dbContext.Networks
            .Where(n => networkIdsList.Contains(n.Id))
            .ToListAsync(cancellationToken);

        if (networks.Count == 0)
            return Error.NotFoundFor(nameof(Network), networkIdsList.FirstOrDefault());

        var containerId = await GetContainerIdByOwnerAsync(serviceId, cancellationToken);
        var isSidecar = await IsSidecarAsync(serviceId, cancellationToken);

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

            var connection = await GetOrCreateConnectionAsync(isSidecar, serviceId, network.Id, cancellationToken);

            if (containerId == null || string.IsNullOrEmpty(network.DockerNetworkId))
            {
                _logger.LogInformation(
                    "Recorded desired network membership for service {ServiceId} on network {NetworkId}; " +
                    "no live container to connect yet, will apply on next deploy.",
                    serviceId,
                    network.Id);
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

                var assignedIp = await TryGetAssignedIpAsync(network.DockerNetworkId, containerId, cancellationToken);
                if (assignedIp is not null)
                    connection.AssignIpAddress(assignedIp);

                _logger.LogInformation(
                    "Connected service {ServiceId} (container {ContainerId}) to network {NetworkId}",
                    serviceId, containerId, network.Id);
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogDebug(
                    "Service {ServiceId} container {ContainerId} is already connected to network {NetworkId}",
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

            return Error.Docker.OperationFailed(string.Join(" ", errors));
        }

        return Result.Success();
    }

    public async Task<Result> DisconnectServiceFromNetworksAsync(Guid serviceId, IEnumerable<Guid> networkIds,
        CancellationToken cancellationToken)
    {
        var networkIdsList = networkIds.ToList();
        var isSidecar = await IsSidecarAsync(serviceId, cancellationToken);

        var connections = await GetConnectionsAsync(isSidecar, serviceId, networkIdsList, cancellationToken);

        if (connections.Count == 0)
        {
            _logger.LogWarning(
                "No network connections found for service {ServiceId} when attempting to disconnect from specified networks.",
                serviceId);
            return Result.Success();
        }

        var containerId = await GetContainerIdByOwnerAsync(serviceId, cancellationToken);

        var errors = new List<string>();
        if (containerId is not null)
        {
            foreach (var (connection, network) in connections)
            {
                try
                {
                    await _dockerClient.Networks.DisconnectNetworkAsync(
                        network!.DockerNetworkId,
                        new NetworkDisconnectParameters
                        {
                            Container = containerId,
                            Force = true
                        },
                        cancellationToken);

                    _logger.LogInformation(
                        "Disconnected service {ServiceId} (container {ContainerId}) from network {NetworkId}",
                        serviceId, containerId, connection.NetworkId);
                }
                catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug(
                        "Network {NetworkId} not found when disconnecting service {ServiceId}; may have been deleted",
                        network!.DockerNetworkId,
                        serviceId);
                }
                catch (DockerApiException ex)
                {
                    var errorMsg = $"Failed to disconnect from network {network!.Name}: {ex.Message}";
                    errors.Add(errorMsg);

                    _logger.LogWarning(
                        ex,
                        "Docker API error disconnecting service {ServiceId} from network {NetworkId}: {ErrorMessage}",
                        serviceId,
                        network.DockerNetworkId,
                        ex.Message);
                }
            }
        }

        RemoveConnections(isSidecar, connections.Select(c => c.Connection));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} disconnected from {NetworkCount} networks",
            serviceId,
            connections.Count);

        if (errors.Any())
        {
            _logger.LogWarning(
                "Disconnection completed with {ErrorCount} Docker API errors for service {ServiceId}",
                errors.Count,
                serviceId);

            return Error.Docker.OperationFailed(string.Join(" ", errors));
        }

        return Result.Success();
    }

    public async Task<Result> DisconnectServiceFromAllNetworksAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var isSidecar = await IsSidecarAsync(serviceId, cancellationToken);
        var connections = await GetConnectionsAsync(isSidecar, serviceId, networkIds: null, cancellationToken);

        if (connections.Count <= 0)
        {
            _logger.LogWarning(
                "No network connections found for service {ServiceId} when attempting to disconnect from all networks.",
                serviceId);
            return Result.Success();
        }

        var containerId = await GetContainerIdByOwnerAsync(serviceId, cancellationToken);

        var errors = new List<string>();
        if (containerId is not null)
        {
            foreach (var (connection, network) in connections)
            {
                try
                {
                    await _dockerClient.Networks.DisconnectNetworkAsync(
                        network!.DockerNetworkId,
                        new NetworkDisconnectParameters
                        {
                            Container = containerId,
                            Force = true
                        },
                        cancellationToken);

                    _logger.LogInformation(
                        "Disconnected service {ServiceId} (container {ContainerId}) from network {NetworkId}",
                        serviceId, containerId, connection.NetworkId);
                }
                catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug(
                        "Network {NetworkId} not found when disconnecting service {ServiceId}; may have been deleted",
                        network!.DockerNetworkId,
                        serviceId);
                }
                catch (DockerApiException ex)
                {
                    var errorMsg = $"Failed to disconnect from network {network!.Name}: {ex.Message}";
                    errors.Add(errorMsg);

                    _logger.LogWarning(
                        ex,
                        "Docker API error disconnecting service {ServiceId} from network {NetworkId}: {ErrorMessage}",
                        serviceId,
                        network.DockerNetworkId,
                        ex.Message);
                }
            }
        }

        // The container is being torn down, but only its membership in the auto-managed
        // ProjectEnvironment network is tied to that container's lifecycle. Shared/External network
        // assignments are a user-configured desired state that must survive stop/restart/redeploy,
        // so those rows are kept (with their now-stale IP cleared) instead of being deleted - the
        // next deploy/start reconnects them to the new container.
        var projectEnvironmentConnections = connections
            .Where(c => c.Network!.Type == NetworkType.ProjectEnvironment)
            .Select(c => c.Connection)
            .ToList();
        var persistentConnections = connections
            .Where(c => c.Network!.Type != NetworkType.ProjectEnvironment)
            .Select(c => c.Connection);

        if (projectEnvironmentConnections.Count > 0)
            RemoveConnections(isSidecar, projectEnvironmentConnections);

        foreach (var connection in persistentConnections)
            connection.ClearIpAddress();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Service {ServiceId} disconnected from {NetworkCount} networks",
            serviceId,
            connections.Count);

        if (errors.Any())
        {
            _logger.LogWarning(
                "Disconnection completed with {ErrorCount} Docker API errors for service {ServiceId}",
                errors.Count,
                serviceId);
        }

        return Result.Success();
    }

    public async Task<Result> EnsureNetworkExistsAsync(Guid networkId, CancellationToken cancellationToken)
    {
        var network = await _dbContext.Networks.FirstOrDefaultAsync(n => n.Id == networkId, cancellationToken);
        if (network == null) return Error.NotFoundFor(nameof(Network), networkId);

        return await EnsureNetworkExistsAsync(network, cancellationToken);
    }

    public async Task<Result> DeleteNetworkAsync(Guid networkId, CancellationToken cancellationToken)
    {
        var network = await _dbContext.Networks.FirstOrDefaultAsync(n => n.Id == networkId, cancellationToken);
        if (network == null) return Error.NotFoundFor(nameof(Network), networkId);

        if (string.IsNullOrEmpty(network.DockerNetworkId))
            return Result.Success();

        try
        {
            await _dockerClient.Networks.DeleteNetworkAsync(network.DockerNetworkId, cancellationToken);
            _logger.LogInformation("Deleted Docker network {DockerNetworkId} for network {NetworkId}", network.DockerNetworkId, network.Id);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Docker network {DockerNetworkId} already gone when deleting network {NetworkId}", network.DockerNetworkId, network.Id);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Docker API error while deleting network {DockerNetworkId} for network {NetworkId}: {ErrorMessage}",
                network.DockerNetworkId, network.Id, ex.Message);
            return Error.Docker.FailedToCreateNetwork;
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
            // Docker's "name" list filter does a substring match, not an exact match, so we must
            // re-filter for an exact name before trusting a result - otherwise this can silently
            // adopt an unrelated network (e.g. one whose name merely contains this one's) as if it
            // were this network's Docker network.
            var existingNetworks = await _dockerClient.Networks.ListNetworksAsync(
                new NetworksListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        { "name", new Dictionary<string, bool> { { networkName, true } } }
                    }
                },
                cancellationToken);

            var exactMatch = existingNetworks.FirstOrDefault(n => n.Name == networkName);
            if (exactMatch is not null)
            {
                _logger.LogInformation(
                    "Found existing Docker network '{NetworkName}' for network {NetworkId}, updating Docker network ID",
                    networkName, network.Id);
                network.SetDockerNetworkId(exactMatch.ID);
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

                var ipam = await TryBuildIpamAsync(network.ProjectId.Value, network.EnvironmentId.Value, cancellationToken);

                var createResponse = await _dockerClient.Networks.CreateNetworkAsync(
                    new NetworksCreateParameters
                    {
                        Name = networkName,
                        Driver = "bridge",
                        Attachable = true,
                        CheckDuplicate = true,
                        IPAM = ipam is null
                            ? null
                            : new IPAM
                            {
                                Driver = "default",
                                Config = [new IPAMConfig { Subnet = ipam.Value.Subnet, Gateway = ipam.Value.Gateway }]
                            },
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
                if (ipam is not null)
                    network.AssignNetworkInfo(ipam.Value.Subnet, ipam.Value.Gateway);
                _dbContext.Networks.Update(network);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully created Docker network {NetworkId} for network {NetworkId}",
                    createResponse.ID, network.Id);

                return Result.Success();
            }

            if (network.Type is NetworkType.Shared or NetworkType.System)
            {
                var networkTypeLabel = network.Type == NetworkType.System ? "system" : "shared";

                _logger.LogInformation(
                    "Network '{NetworkName}' does not exist in Docker, creating it for {NetworkType} network {NetworkId}",
                    networkName, networkTypeLabel, network.Id);

                var createResponse = await _dockerClient.Networks.CreateNetworkAsync(
                    new NetworksCreateParameters
                    {
                        Name = networkName,
                        Driver = "bridge",
                        Attachable = true,
                        CheckDuplicate = true,
                        Labels = new Dictionary<string, string>
                        {
                            { "haven.network-type", networkTypeLabel },
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
                    "Successfully created Docker network {DockerNetworkId} for {NetworkType} network {NetworkId}",
                    createResponse.ID, networkTypeLabel, network.Id);

                return Result.Success();
            }

            return Error.Docker.NetworkNotFound;
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(
                ex,
                "Docker API error while ensuring network {NetworkName} (ID: {NetworkId}) exists: {ErrorMessage}",
                networkName, network.Id, ex.Message);
            return Error.Docker.FailedToCreateNetwork;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error ensuring network {NetworkName} (ID: {NetworkId}) exists",
                networkName, network.Id);
            return Error.Docker.NetworkNotFound;
        }
    }

    private async Task<string?> TryGetAssignedIpAsync(string dockerNetworkId, string containerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var networkResponse = await _dockerClient.Networks.InspectNetworkAsync(dockerNetworkId, cancellationToken);
            if (networkResponse.Containers is null || !networkResponse.Containers.TryGetValue(containerId, out var endpoint))
                return null;

            var ipv4Address = endpoint.IPv4Address;
            if (string.IsNullOrWhiteSpace(ipv4Address))
                return null;

            return ipv4Address.Split('/')[0];
        }
        catch (DockerApiException ex)
        {
            _logger.LogWarning(ex, "Failed to inspect network {DockerNetworkId} to resolve assigned IP for container {ContainerId}",
                dockerNetworkId, containerId);
            return null;
        }
    }

    private async Task<(string Subnet, string Gateway)?> TryBuildIpamAsync(Guid projectId, Guid environmentId,
        CancellationToken cancellationToken)
    {
        var subnet = DockerUtils.GenerateSubnetForEnvironment(projectId, environmentId);
        var gateway = DockerUtils.DeriveGatewayFromSubnet(subnet);

        try
        {
            var existingNetworks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            var collides = existingNetworks.Any(n => n.IPAM?.Config?.Any(c => c.Subnet == subnet) == true);
            if (collides)
            {
                _logger.LogWarning(
                    "Generated subnet {Subnet} collides with an existing Docker network; falling back to automatic IPAM allocation.",
                    subnet);
                return null;
            }

            return (subnet, gateway);
        }
        catch (DockerApiException ex)
        {
            _logger.LogWarning(ex, "Failed to check for subnet collisions; falling back to automatic IPAM allocation.");
            return null;
        }
    }

    /// <summary>
    /// Network connect/disconnect is shared between Service and Sidecar owners, but each persists its
    /// membership in its own join table (<c>service_networks</c> vs <c>sidecar_networks</c>) since they're
    /// separate aggregates - this distinguishes which table an owner id belongs to.
    /// </summary>
    private async Task<bool> IsSidecarAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await _dbContext.Sidecars.AnyAsync(s => s.Id == ownerId, cancellationToken);

    /// <summary>
    /// Fetches an owner's existing network-membership rows (all of them if <paramref name="networkIds"/>
    /// is null, or only those matching it) from whichever join table applies, as <see cref="INetworkConnection"/>
    /// so callers don't need to type-switch. Tracked (not AsNoTracking): <see cref="ISidecarRepository"/> already
    /// eager-loads <c>SidecarNetworks</c>, and re-querying untracked here would create a duplicate instance
    /// that conflicts with the already-tracked one on save.
    /// </summary>
    private async Task<List<(INetworkConnection Connection, Network? Network)>> GetConnectionsAsync(
        bool isSidecar, Guid ownerId, IReadOnlyCollection<Guid>? networkIds, CancellationToken cancellationToken)
    {
        if (isSidecar)
        {
            var query = _dbContext.SidecarNetworks.Include(sn => sn.Network).Where(sn => sn.SidecarId == ownerId);
            if (networkIds is not null) query = query.Where(sn => networkIds.Contains(sn.NetworkId));
            var rows = await query.ToListAsync(cancellationToken);
            return rows.Select(sn => ((INetworkConnection)sn, sn.Network)).ToList();
        }
        else
        {
            var query = _dbContext.ServiceNetworks.Include(sn => sn.Network).Where(sn => sn.ServiceId == ownerId);
            if (networkIds is not null) query = query.Where(sn => networkIds.Contains(sn.NetworkId));
            var rows = await query.ToListAsync(cancellationToken);
            return rows.Select(sn => ((INetworkConnection)sn, sn.Network)).ToList();
        }
    }

    private async Task<INetworkConnection> GetOrCreateConnectionAsync(
        bool isSidecar, Guid ownerId, Guid networkId, CancellationToken cancellationToken)
    {
        if (isSidecar)
        {
            var connection = await _dbContext.SidecarNetworks
                .FirstOrDefaultAsync(sn => sn.SidecarId == ownerId && sn.NetworkId == networkId, cancellationToken);
            if (connection is null)
            {
                connection = SidecarNetwork.Create(ownerId, networkId);
                _dbContext.SidecarNetworks.Add(connection);
            }
            return connection;
        }
        else
        {
            var connection = await _dbContext.ServiceNetworks
                .FirstOrDefaultAsync(sn => sn.ServiceId == ownerId && sn.NetworkId == networkId, cancellationToken);
            if (connection is null)
            {
                connection = ServiceNetwork.Create(ownerId, networkId);
                _dbContext.ServiceNetworks.Add(connection);
            }
            return connection;
        }
    }

    private void RemoveConnections(bool isSidecar, IEnumerable<INetworkConnection> connections)
    {
        if (isSidecar)
            _dbContext.SidecarNetworks.RemoveRange(connections.Cast<SidecarNetwork>());
        else
            _dbContext.ServiceNetworks.RemoveRange(connections.Cast<ServiceNetwork>());
    }

    private async Task<string?> GetContainerIdByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var label = DockerUtils.BuildIdLabel(ownerId);

            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true,
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
                _logger.LogDebug("No container found for owner {OwnerId}", ownerId);
                return null;
            }

            var containerId = containers.First().ID;
            _logger.LogDebug("Found container {ContainerId} for owner {OwnerId}", containerId, ownerId);

            return containerId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving container ID for owner {OwnerId}", ownerId);
            return null;
        }
    }
}