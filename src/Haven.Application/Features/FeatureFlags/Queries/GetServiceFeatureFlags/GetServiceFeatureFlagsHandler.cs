using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.FeatureFlags.Queries.GetServiceFeatureFlags;

public class GetServiceFeatureFlagsHandler(IFeatureFlagRepository repository) : IPagedQueryHandler<GetServiceFeatureFlagsQuery, FeatureFlagDto>
{
    public async ValueTask<PagedResult<FeatureFlagDto>> Handle(GetServiceFeatureFlagsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetForServicePagedAsync(query.ServiceId, query.PageNumber, query.PageSize, cancellationToken);
        return result.Project(ff => ff.ToDto());
    }
}