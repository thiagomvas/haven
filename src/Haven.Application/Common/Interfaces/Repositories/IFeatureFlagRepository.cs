using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IFeatureFlagRepository
{
    Task<PagedResult<FeatureFlag>> GetForServicePagedAsync(Guid serviceId, int page, int pageSize, CancellationToken cancellationToken);
    IAsyncEnumerable<FeatureFlag> GetForServiceAsync(Guid serviceId);
    Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<FeatureFlag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
        
    Task<IReadOnlyList<FeatureFlag>> GetForServiceListAsync(Guid serviceId, CancellationToken cancellationToken);
    Task AddAsync(FeatureFlag featureFlag, CancellationToken cancellationToken);
    Task AddAsync(IEnumerable<FeatureFlag> featureFlags, CancellationToken cancellationToken);
    Task RemoveAsync(FeatureFlag featureFlag, CancellationToken cancellationToken);
    Task CleanForServiceAsync(Guid serviceId, CancellationToken cancellationToken);
}