using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Commands.UpdateEnvironment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class UpdateEnvironmentEndpoint(IMediator mediator) : Endpoint<UpdateEnvironmentCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}");
        AllowAnonymous();
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Update an environment";
            s.Description = "Partially updates an environment within a project.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(UpdateEnvironmentCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}