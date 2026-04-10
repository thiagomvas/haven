using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IProjectRepository
{
    Task<Guid> AddAsync(Project project, CancellationToken cancellationToken);
}