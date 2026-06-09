using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Queries.GetProject;
using Haven.Application.Features.Projects.Queries.GetProjects;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class GetProjectEndpoint(IMediator mediator)
    : Endpoint<GetProjectQuery, ApiResponse<ProjectDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}");
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Get project";
            s.Description = "Returns a project by ID.";
            s[200] = "OK";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(GetProjectQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}