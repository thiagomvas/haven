using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Queries;
using Haven.Application.Features.Environments.Queries.GetEnvironment;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class GetEnvironmentEndpoint(IMediator mediator)
    : Endpoint<GetEnvironmentQuery, ApiResponse<EnvironmentDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}");
        AllowAnonymous();
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Get environment";
            s.Description = "Returns an environment by ID.";
            s[200] = "OK";
            s[404] = "Project or environment not found";
        });
    }

    public override async Task HandleAsync(GetEnvironmentQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
