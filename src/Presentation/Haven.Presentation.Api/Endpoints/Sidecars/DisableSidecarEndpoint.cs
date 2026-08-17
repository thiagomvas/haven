using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Sidecars.Commands.DisableSidecar;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class DisableSidecarEndpoint(IMediator mediator)
    : Endpoint<DisableSidecarCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/sidecars/{sidecarId}/disable");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Disables a sidecar";
            s.Description = "Stops and disables a sidecar.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Sidecar not found";
        });
    }

    public override async Task HandleAsync(DisableSidecarCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}