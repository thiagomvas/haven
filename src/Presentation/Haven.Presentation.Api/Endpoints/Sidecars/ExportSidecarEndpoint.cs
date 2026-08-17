using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Sidecars.Commands.ExportSidecarManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class ExportSidecarEndpoint(IMediator mediator)
    : Endpoint<ExportSidecarManifestCommand, ApiResponse<string>>
{
    public override void Configure()
    {
        Post("/sidecars/{sidecarId}/export");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Exports a sidecar's manifest";
            s.Description = "Force re-writes the sidecar's manifest file on disk from its current database state " +
                "and returns the written YAML content.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Sidecar not found";
        });
    }

    public override async Task HandleAsync(ExportSidecarManifestCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
