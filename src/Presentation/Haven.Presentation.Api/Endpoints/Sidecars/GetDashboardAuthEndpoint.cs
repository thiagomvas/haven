using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration;
using Haven.Application.Features.Configuration.Queries.GetTraefikDashboardAuth;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class GetDashboardAuthEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<TraefikDashboardAuthDto>>
{
    public override void Configure()
    {
        Get("/sidecars/traefik/dashboard-auth");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Get the Traefik dashboard's Basic Auth status";
            s.Description = "Returns whether the Traefik dashboard requires Basic Auth and, if so, the configured username. Never returns the password/hash.";
            s[200] = "Success";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetTraefikDashboardAuthQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}