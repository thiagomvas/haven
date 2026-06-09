using FastEndpoints;

using Haven.Application.Common;
using Haven.Application.Features.Projects.Commands.DeleteProject;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class DeleteProjectEndpoint(IMediator mediator) : Endpoint<DeleteProjectCommand>
{
    public override void Configure()
    {
        Delete("/projects/{id}");
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Delete a project";
            s.Description = "Permanently deletes a project by ID.";
            s[204] = "Deleted";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(DeleteProjectCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}