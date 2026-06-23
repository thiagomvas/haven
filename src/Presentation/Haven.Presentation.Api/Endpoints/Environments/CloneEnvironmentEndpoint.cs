using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Commands.CloneEnvironment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class CloneEnvironmentEndpoint(IMediator mediator) : Endpoint<CloneEnvironmentCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/clone");
        
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Clone an environment";
            s.Description = "Creates an exact copy of an environment including all its services and environment variables. Can optionally clone into a different project.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Project or environment not found";
            s[409] = "An environment with the new name already exists in the target project";
        });
    }

    public override async Task HandleAsync(CloneEnvironmentCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}