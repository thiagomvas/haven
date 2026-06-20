using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<Service?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<IReadOnlyList<Service>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken);
    Task AddAsync(Service service, CancellationToken cancellationToken);
    IAsyncEnumerable<Service> GetAsync(CancellationToken cancellationToken);
}