using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Users.Commands.SetUserPermissions;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Users;

public sealed class SetUserPermissionsEndpoint(IMediator mediator) : Endpoint<SetUserPermissionsCommand, ApiResponse>
{
    public override void Configure()
    {
        Put("/users/{id}/permissions");
        Options(x => x.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Set user permissions";
            s.Description = "Replaces all permissions for a user. Admin only.";
            s[200] = "Permissions updated";
            s[400] = "Validation error";
            s[403] = "Forbidden — admin only";
            s[404] = "User not found";
        });
    }

    public override async Task HandleAsync(SetUserPermissionsCommand req, CancellationToken ct)
    {
        req.UserId = Route<Guid>("id");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
