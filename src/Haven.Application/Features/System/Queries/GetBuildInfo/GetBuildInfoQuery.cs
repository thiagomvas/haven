using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetBuildInfo;

[AuthenticatedOnly]
public sealed class GetBuildInfoQuery : IQuery<BuildInfoDto>;