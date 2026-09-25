using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Secrets.Commands.CreateSecretVariable;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class CreateEnvironmentSecretEndpoint(IMediator mediator) : Endpoint<CreateSecretVariableCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/secrets");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "Create an environment secret";
            s.Description = "Creates a new secret variable scoped to an environment.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[409] = "A secret with this key already exists for the environment";
        });
    }

    public override async Task HandleAsync(CreateSecretVariableCommand req, CancellationToken ct)
    {
        req.ParentId = Route<Guid>("environmentId");
        req.ParentType = EnvironmentVariableParentType.Environment;

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
