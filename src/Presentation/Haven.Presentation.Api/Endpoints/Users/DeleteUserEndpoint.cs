using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Users.Commands.DeleteUser;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Users;

public sealed class DeleteUserEndpoint(IMediator mediator) : Endpoint<DeleteUserCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/users/{id}");
        Options(x => x.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Delete a user";
            s.Description = "Permanently deletes a user account. Cannot delete your own account.";
            s[200] = "Deleted";
            s[400] = "Cannot delete own account";
            s[403] = "Forbidden";
            s[404] = "User not found";
        });
    }

    public override async Task HandleAsync(DeleteUserCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
