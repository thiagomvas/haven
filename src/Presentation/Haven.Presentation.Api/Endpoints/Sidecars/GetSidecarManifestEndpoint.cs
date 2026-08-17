using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Sidecars.Queries.GetSidecarManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class GetSidecarManifestEndpoint(IMediator mediator)
    : Endpoint<GetSidecarManifestQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/sidecars/{sidecarId}/manifest");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "Gets a sidecar's manifest";
            s.Description = "Returns the raw YAML content of the sidecar's manifest file on disk.";
            s[200] = "Success";
            s[404] = "Sidecar or manifest file not found";
        });
    }

    public override async Task HandleAsync(GetSidecarManifestQuery req, CancellationToken ct)
    {
        req.SidecarId = Route<Guid>("sidecarId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}