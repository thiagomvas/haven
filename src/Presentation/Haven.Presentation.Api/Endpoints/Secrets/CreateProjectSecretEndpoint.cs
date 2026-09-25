using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Secrets.Commands.CreateSecretVariable;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class CreateProjectSecretEndpoint(IMediator mediator) : Endpoint<CreateSecretVariableCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/secrets");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "Create a project secret";
            s.Description = "Creates a new secret variable scoped to a project.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[409] = "A secret with this key already exists for the project";
        });
    }

    public override async Task HandleAsync(CreateSecretVariableCommand req, CancellationToken ct)
    {
        req.ParentId = Route<Guid>("projectId");
        req.ParentType = EnvironmentVariableParentType.Project;

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
