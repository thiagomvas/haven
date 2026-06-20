using FastEndpoints;

using Haven.Application.Features.Environments.Commands.DeleteEnvironment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class DeleteEnvironmentEndpoint(IMediator mediator) : Endpoint<DeleteEnvironmentCommand>
{
    public override void Configure()
    {
        Delete("/projects/{projectId}/environments/{environmentId}");
        AllowAnonymous();
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Delete an environment";
            s.Description = "Permanently deletes an environment from a project.";
            s[204] = "Deleted";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(DeleteEnvironmentCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        req.EnvironmentId = Route<Guid>("environmentId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}