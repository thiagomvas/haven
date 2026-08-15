using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface ISidecarRepository
{
    Task<Sidecar?> GetByIdAsync(Guid sidecarId, CancellationToken cancellationToken);
    Task<Sidecar?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<Sidecar>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Sidecar sidecar, CancellationToken cancellationToken);
    Task RemoveAsync(Sidecar sidecar, CancellationToken cancellationToken);
}
