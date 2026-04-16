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
    }

    public override async Task HandleAsync(GetProjectsQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
