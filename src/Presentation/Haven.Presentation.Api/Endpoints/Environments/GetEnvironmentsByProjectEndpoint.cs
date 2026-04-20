using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Queries;
using Haven.Application.Features.Environments.Queries.GetEnvironmentsByProject;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class GetEnvironmentsByProjectEndpoint(IMediator mediator)
    : Endpoint<GetEnvironmentsByProjectQuery, ApiResponse<IReadOnlyList<EnvironmentDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments");
        AllowAnonymous();
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "List environments";
            s.Description = "Returns all environments belonging to a project.";
            s[200] = "OK";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(GetEnvironmentsByProjectQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
