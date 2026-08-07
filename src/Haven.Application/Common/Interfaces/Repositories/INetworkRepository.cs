using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface INetworkRepository
{
    Task AddAsync(Network network, CancellationToken cancellationToken);
    Task<Network?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Network>> GetByProjectAndEnvironmentAsync(Guid projectId, Guid environmentId, CancellationToken cancellationToken);
    Task<List<Network>> GetAllAsync(NetworkType? type, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}