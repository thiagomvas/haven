using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Jobs.Queries.GetJobInfos;

[RequirePermission(Permissions.Jobs.Read)]
public class GetJobInfosQuery : IQuery<IEnumerable<JobInfo>>
{

}