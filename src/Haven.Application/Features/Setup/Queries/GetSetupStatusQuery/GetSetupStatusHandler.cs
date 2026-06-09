using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Setup.Queries.GetSetupStatusQuery;

public class GetSetupStatusHandler(IHavenService havenService) : IQueryHandler<GetSetupStatusQuery, GetSetupStatusResult>
{
    public async ValueTask<Result<GetSetupStatusResult>> Handle(GetSetupStatusQuery query, CancellationToken cancellationToken)
    {
        var stage = await havenService.GetSetupStageAsync(cancellationToken);
        return new GetSetupStatusResult(stage);
    }
}