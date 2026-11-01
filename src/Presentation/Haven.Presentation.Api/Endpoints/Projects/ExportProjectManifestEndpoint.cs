using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Queries.ExportProjectManifest;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class ExportProjectManifestEndpoint(IMediator mediator)
    : Endpoint<ExportProjectManifestQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/export/manifest");
        Options(x => x.WithTags("Projects"));
        Summary(s =>
        {
            s.Summary = "Export project manifest";
            s.Description = "Exports a project as a YAML manifest, optionally including its environments and their services.";
            s[200] = "OK";
            s[404] = "Project not found";
        });
    }

    public override async Task HandleAsync(ExportProjectManifestQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
