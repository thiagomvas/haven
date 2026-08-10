using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common.Interfaces.Deployment;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Docker;

/// <summary>
/// Runs on a Hangfire recurring schedule (see <see cref="NetworkReconciliationScheduler"/>) to
/// backfill/refresh subnet, gateway, and per-service IP data from Docker for networks/connections
/// Haven already knows about. Entirely best-effort: any Docker error for an individual network or
/// container is logged and skipped, never thrown, so one bad entry can't fail the whole run.
/// </summary>
public sealed class NetworkReconciliationService(
    HavenDbContext dbContext,
    IDockerClient dockerClient,
    ILogger<NetworkReconciliationService> logger)
    : INetworkReconciliationService
{
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        await ReconcileNetworkSubnetsAsync(cancellationToken);
        await ReconcileServiceIpAddressesAsync(cancellationToken);
    }

    private async Task ReconcileNetworkSubnetsAsync(CancellationToken cancellationToken)
    {
        var networks = await dbContext.Networks
            .Where(n => n.DockerNetworkId != null && (n.Subnet == null || n.Gateway == null))
            .ToListAsync(cancellationToken);

        if (networks.Count == 0)
            return;

        var updated = 0;
        foreach (var network in networks)
        {
            try
            {
                var response = await dockerClient.Networks.InspectNetworkAsync(network.DockerNetworkId!, cancellationToken);
                var config = response.IPAM?.Config?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Subnet));
                if (config is null || string.IsNullOrWhiteSpace(config.Gateway))
                {
                    logger.LogDebug("No IPAM config found for network {NetworkId} ({DockerNetworkId})", network.Id, network.DockerNetworkId);
                    continue;
                }

                network.AssignNetworkInfo(config.Subnet, config.Gateway);
                updated++;
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogDebug("Docker network {DockerNetworkId} for network {NetworkId} no longer exists; skipping", network.DockerNetworkId, network.Id);
            }
            catch (DockerApiException ex)
            {
                logger.LogWarning(ex, "Failed to inspect Docker network {DockerNetworkId} for network {NetworkId}", network.DockerNetworkId, network.Id);
            }
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Network reconciliation: backfilled subnet/gateway for {Count} network(s)", updated);
        }
    }

    /// <summary>
    /// For every network a service is supposed to be a member of, verifies the service's current
    /// container is actually attached in Docker and re-attaches it if not. This is what heals
    /// drift between Haven's desired state (<c>ServiceNetworks</c> rows) and Docker's actual state -
    /// e.g. a container that crash-restarted via its own restart policy without ever completing its
    /// initial network connect, or any other transient connect failure at deploy time. Also backfills
    /// the recorded IP address once the attachment is confirmed.
    /// </summary>
    private async Task ReconcileServiceIpAddressesAsync(CancellationToken cancellationToken)
    {
        var connections = await dbContext.ServiceNetworks
            .Include(sn => sn.Network)
            .Where(sn => sn.Network != null && sn.Network.DockerNetworkId != null)
            .ToListAsync(cancellationToken);

        if (connections.Count == 0)
            return;

        var updated = 0;
        foreach (var connection in connections)
        {
            var containerId = await TryFindContainerIdAsync(connection.ServiceId, cancellationToken);
            if (containerId is null)
                continue;

            var dockerNetworkId = connection.Network!.DockerNetworkId!;

            try
            {
                var response = await dockerClient.Networks.InspectNetworkAsync(dockerNetworkId, cancellationToken);

                if (response.Containers is null || !response.Containers.TryGetValue(containerId, out var endpoint))
                {
                    logger.LogInformation(
                        "Network reconciliation: service {ServiceId}'s container is not attached to network {NetworkId}; reconnecting",
                        connection.ServiceId, connection.NetworkId);

                    await dockerClient.Networks.ConnectNetworkAsync(
                        dockerNetworkId,
                        new NetworkConnectParameters { Container = containerId, EndpointConfig = new EndpointSettings() },
                        cancellationToken);

                    response = await dockerClient.Networks.InspectNetworkAsync(dockerNetworkId, cancellationToken);
                    if (response.Containers is null || !response.Containers.TryGetValue(containerId, out endpoint))
                        continue;

                    updated++;
                }

                if (string.IsNullOrWhiteSpace(endpoint.IPv4Address))
                    continue;

                var ipAddress = endpoint.IPv4Address.Split('/')[0];
                if (connection.IpAddress != ipAddress)
                {
                    connection.AssignIpAddress(ipAddress);
                    updated++;
                }
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogDebug(
                    "Docker network {DockerNetworkId} no longer exists while reconciling service {ServiceId}'s network membership",
                    dockerNetworkId, connection.ServiceId);
            }
            catch (DockerApiException ex)
            {
                logger.LogWarning(ex,
                    "Failed to reconcile service {ServiceId}'s membership on network {DockerNetworkId}",
                    connection.ServiceId, dockerNetworkId);
            }
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Network reconciliation: reconnected/backfilled {Count} service network connection(s)", updated);
        }
    }

    private async Task<string?> TryFindContainerIdAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        try
        {
            var label = DockerUtils.BuildIdLabel(serviceId);
            var containers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = false,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        { "label", new Dictionary<string, bool> { { label.Key + "=" + label.Value, true } } }
                    }
                },
                cancellationToken);

            return containers.Count == 0 ? null : containers[0].ID;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error resolving container for service {ServiceId} during network reconciliation", serviceId);
            return null;
        }
    }
}