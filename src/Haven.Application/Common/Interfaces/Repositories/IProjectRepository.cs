using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;


namespace Haven.Application.Common.Interfaces.Repositories;

public interface IProjectRepository
{
    Task<Guid> AddAsync(Project project, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task<Project?> GetByIdWithEnvironmentsAsync(Guid projectId, CancellationToken cancellationToken);
    Task<Project?> GetByIdWithServicesAsync(Guid projectId, CancellationToken cancellationToken);
    public Task<Project?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<bool> ExistsWithNameAsync(string name, Guid excludeId, CancellationToken cancellationToken);
    Task<PagedResult<Project>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    void Remove(Project project);
}