using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetLatestVersion;

[AuthenticatedOnly]
public sealed class GetLatestVersionQuery : IQuery<LatestVersionDto>;