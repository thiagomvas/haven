using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Setup.Queries.GetSetupStatusQuery;

[AnonymousAccess]
public sealed class GetSetupStatusQuery : IQuery<GetSetupStatusResult>;