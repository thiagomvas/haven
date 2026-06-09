using FastEndpoints;

using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Commands.UpdateProject;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class UpdateProjectEndpoint(IMediator mediator) : Endpoint<UpdateProjectCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/projects/{id}");
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Update a project";
            s.Description = "Partially updates a project by ID.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(UpdateProjectCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}