using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Secrets.Commands.UpdateSecretVariable;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class UpdateSecretVariableEndpoint(IMediator mediator) : Endpoint<UpdateSecretVariableCommand, ApiResponse>
{
    public override void Configure()
    {
        Patch("/secrets/{id}");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "Update a secret";
            s.Description = "Partially updates a secret variable's key or value.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Secret not found";
            s[409] = "A secret with this key already exists for the parent";
        });
    }

    public override async Task HandleAsync(UpdateSecretVariableCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
