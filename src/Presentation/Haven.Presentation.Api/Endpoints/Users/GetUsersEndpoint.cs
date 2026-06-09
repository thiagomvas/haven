using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Users;
using Haven.Application.Features.Users.Queries.GetUsers;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Users;

public sealed class GetUsersEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<List<UserDto>>>
{
    public override void Configure()
    {
        Get("/users");
        Options(x => x.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Get all users";
            s.Description = "Returns a list of all user accounts.";
            s[200] = "Users list";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetUsersQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}