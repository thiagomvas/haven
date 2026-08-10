using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface INetworkingService
{
    ServiceType ServiceType { get; }
    Task<Result> CreateProjectEnvironmentNetworkAsync(Guid projectId, Guid environmentId, CancellationToken cancellationToken);
    Task<Result> ConnectServiceToNetworksAsync(Guid serviceId, IEnumerable<Guid> networkIds, CancellationToken cancellationToken);
    Task<Result> DisconnectServiceFromNetworksAsync(Guid serviceId, IEnumerable<Guid> networkIds, CancellationToken cancellationToken);
    Task<Result> DisconnectServiceFromAllNetworksAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<Result> EnsureNetworkExistsAsync(Guid networkId, CancellationToken cancellationToken);
    Task<Result> DeleteNetworkAsync(Guid networkId, CancellationToken cancellationToken);
}