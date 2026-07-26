using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Queries.ResolveEnvironment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class ResolveEnvironmentEndpoint(IMediator mediator)
    : Endpoint<ResolveEnvironmentQuery, ApiResponse<EnvironmentLocationDto>>
{
    public override void Configure()
    {
        Get("/environments/{environmentId}");

        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Resolve environment location";
            s.Description = "Returns the project ID for an environment given only its ID.";
            s[200] = "OK";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(ResolveEnvironmentQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
