using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Jobs.Queries.GetJobInfos;

public class GetJobInfosHandler(IJobsService service) : IQueryHandler<GetJobInfosQuery, IEnumerable<JobInfo>>
{
    public async ValueTask<Result<IEnumerable<JobInfo>>> Handle(GetJobInfosQuery query, CancellationToken cancellationToken)
    {
        var jobInfos = await service.GetJobInfosAsync(cancellationToken);
        return Result<IEnumerable<JobInfo>>.Success(jobInfos);
    }
}