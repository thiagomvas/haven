using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Sidecars.Commands.UpdateSidecar;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class UpdateSidecarEndpoint(IMediator mediator)
    : Endpoint<UpdateSidecarCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/sidecars/{sidecarId}");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Updates a sidecar's configuration";
            s.Description = "Partially updates a sidecar's Docker configuration (image, ports, command args, " +
                "restart policy) and redeploys it if it is currently enabled.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Sidecar not found";
        });
    }

    public override async Task HandleAsync(UpdateSidecarCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
