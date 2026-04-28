using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IEnvironmentRepository
{
    Task<Environment?> GetByIdAsync(Guid environmentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Environment>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
}
