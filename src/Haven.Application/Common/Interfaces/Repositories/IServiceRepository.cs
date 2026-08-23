using Haven.Application.Features.Networks.Queries.SearchAttachableServices;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<Service?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<IReadOnlyList<Service>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken);
    Task AddAsync(Service service, CancellationToken cancellationToken);
    IAsyncEnumerable<Service> GetAsync(CancellationToken cancellationToken);
    Task RemoveAsync(Service service, CancellationToken cancellationToken);
    Task<List<Guid>> FilterMissingIdsAsync(List<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Searches services by name, environment name or project name, excluding services already
    /// attached to <paramref name="excludeNetworkId"/>. Used to power the "attach service to network" picker.
    /// </summary>
    Task<List<AttachableServiceDto>> SearchAttachableAsync(
        Guid excludeNetworkId,
        string? search,
        int limit,
        CancellationToken cancellationToken);
}