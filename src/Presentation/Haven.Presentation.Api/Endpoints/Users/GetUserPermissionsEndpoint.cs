using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Users.Queries.GetUserPermissions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Users;

public sealed class GetUserPermissionsEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<string[]>>
{
    public override void Configure()
    {
        Get("/users/{id}/permissions");
        Options(x => x.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Get user permissions";
            s.Description = "Returns the list of permissions assigned to a user.";
            s[200] = "Permissions returned";
            s[404] = "User not found";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetUserPermissionsQuery { UserId = Route<Guid>("id") };
        var result = await mediator.Send(query, ct);
        await this.SendResultAsync(result, ct);
    }
}