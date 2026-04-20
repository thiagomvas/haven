using FastEndpoints;
using Haven.Application.Common.Messaging;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Queries.GetProjects;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class GetProjectsEndpoint(IMediator mediator)
    : Endpoint<GetProjectsQuery, ApiResponse<PagedResult<ProjectDto>>>
{
    public override void Configure()
    {
        Get("/projects");
        AllowAnonymous();
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "List projects";
            s.Description = "Returns a paginated list of all projects.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetProjectsQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
