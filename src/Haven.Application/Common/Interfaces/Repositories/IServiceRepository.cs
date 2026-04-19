using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IServiceRepository
{
    Task<IReadOnlyList<Service>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken);
}
