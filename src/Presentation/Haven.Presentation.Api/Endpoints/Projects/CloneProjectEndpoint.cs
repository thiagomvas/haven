using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Commands.CloneProject;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class CloneProjectEndpoint(IMediator mediator) : Endpoint<CloneProjectCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/clone");

        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Clone a project";
            s.Description = "Creates an exact copy of a project including all its environments, services, and environment variables.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Project not found";
            s[409] = "A project with the new name already exists";
        });
    }

    public override async Task HandleAsync(CloneProjectCommand req, CancellationToken ct)
    {
        req.ProjectId = Route<Guid>("projectId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}