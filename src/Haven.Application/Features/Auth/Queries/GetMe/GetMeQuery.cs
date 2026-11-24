using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Queries.GetMe;

[AuthenticatedOnly]
public sealed class GetMeQuery : IQuery<MeResponse>
{
    public Guid UserId { get; init; }
}