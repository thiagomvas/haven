using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Secrets;
using Haven.Application.Features.Secrets.Queries.GetSecretVariable;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class GetSecretVariableEndpoint(IMediator mediator) : Endpoint<GetSecretVariableQuery, ApiResponse<SecretVariableDto>>
{
    public override void Configure()
    {
        Get("/secrets/{id}");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "Get a secret";
            s.Description = "Gets a single secret variable by ID. The decrypted value is never returned.";
            s[404] = "Secret not found";
        });
    }

    public override async Task HandleAsync(GetSecretVariableQuery req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
