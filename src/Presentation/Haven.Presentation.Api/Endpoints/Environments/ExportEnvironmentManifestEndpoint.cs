using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Environments.Queries.ExportEnvironmentManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Environments;

public sealed class ExportEnvironmentManifestEndpoint(IMediator mediator)
    : Endpoint<ExportEnvironmentManifestQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/export/manifest");
        Options(x => x.WithTags("Environments"));
        Summary(s =>
        {
            s.Summary = "Export environment manifest";
            s.Description = "Exports an environment as a YAML manifest, optionally including its services.";
            s[200] = "OK";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(ExportEnvironmentManifestQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
