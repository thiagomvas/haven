using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Application.Features.Manifests.Commands.SyncFromManifests;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Manifests;

public sealed class SyncFromManifestsEndpoint(IMediator mediator)
    : Endpoint<SyncFromManifestsCommand, ApiResponse<RestoreBackupResult>>
{
    public override void Configure()
    {
        Post("/manifests/sync");

        Options(x => x.WithTags("Manifests"));
        Summary(s =>
        {
            s.Summary = "Sync database from manifests";
            s.Description = "Restores DB state from the live manifests directory. Diff-based (created/updated/deleted by id), same engine as backup restore. Supports dry run to preview changes without applying them.";
            s[200] = "Synchronization completed (or dry run diff returned)";
            s[500] = "Synchronization failed";
        });
    }

    public override async Task HandleAsync(SyncFromManifestsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}