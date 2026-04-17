using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IEnvironmentRepository
{
    Task<IReadOnlyList<Environment>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
}
