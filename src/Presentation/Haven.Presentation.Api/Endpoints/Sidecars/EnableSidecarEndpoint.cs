using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Sidecars.Commands.EnableSidecar;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class EnableSidecarEndpoint(IMediator mediator)
    : Endpoint<EnableSidecarCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/sidecars/{sidecarId}/enable");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Enables a sidecar";
            s.Description = "Enables and deploys a sidecar.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Sidecar not found";
        });
    }

    public override async Task HandleAsync(EnableSidecarCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}