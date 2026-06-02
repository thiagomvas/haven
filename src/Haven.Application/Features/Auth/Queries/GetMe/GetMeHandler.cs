using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.Auth.Queries.GetMe;

public sealed class GetMeHandler(IUserRepository userRepository)
    : IQueryHandler<GetMeQuery, MeResponse>
{
    public async ValueTask<Result<MeResponse>> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
            return Error.NotFoundFor(nameof(user), query.UserId);

        return user.ToMeResponse();
    }
}
