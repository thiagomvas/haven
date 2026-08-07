using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IEnvironmentRepository
{
    Task<Environment?> GetByIdAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Environment>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task AddAsync(Environment environment, CancellationToken cancellationToken);
    IAsyncEnumerable<Environment> GetAsync(CancellationToken cancellationToken);
}