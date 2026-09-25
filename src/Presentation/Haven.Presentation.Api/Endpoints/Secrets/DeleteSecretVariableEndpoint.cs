using FastEndpoints;

using Haven.Application.Features.Secrets.Commands.DeleteSecretVariable;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class DeleteSecretVariableEndpoint(IMediator mediator) : Endpoint<DeleteSecretVariableCommand>
{
    public override void Configure()
    {
        Delete("/secrets/{id}");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "Delete a secret";
            s.Description = "Permanently deletes a secret variable by ID.";
            s[204] = "Deleted";
            s[404] = "Secret not found";
        });
    }

    public override async Task HandleAsync(DeleteSecretVariableCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
