using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration;
using Haven.Application.Features.Configuration.Commands.UpdateTraefikDashboardAuth;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class UpdateDashboardAuthEndpoint(IMediator mediator)
    : Endpoint<UpdateTraefikDashboardAuthCommand, ApiResponse<TraefikDashboardAuthDto>>
{
    public override void Configure()
    {
        Patch("/sidecars/traefik/dashboard-auth");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Update the Traefik dashboard's Basic Auth credentials";
            s.Description = "Enables/disables HTTP Basic Auth on the Traefik dashboard router and rotates its credentials. A blank password when enabling keeps the existing password.";
            s[200] = "Updated";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(UpdateTraefikDashboardAuthCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}