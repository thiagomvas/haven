using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Sidecars.Commands.ImportSidecarManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class ImportSidecarEndpoint(IMediator mediator)
    : Endpoint<ImportSidecarManifestCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/sidecars/{sidecarId}/import");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Imports a sidecar's manifest";
            s.Description = "Applies a YAML manifest to the database entity. If ManifestYaml is provided in the " +
                "request body, that content is used; otherwise the sidecar's manifest file is re-read from disk.";
            s[200] = "Success";
            s[400] = "Validation error";
            s[404] = "Sidecar or manifest file not found";
        });
    }

    public override async Task HandleAsync(ImportSidecarManifestCommand req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}