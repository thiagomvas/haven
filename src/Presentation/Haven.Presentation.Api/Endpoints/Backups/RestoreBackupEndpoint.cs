using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Backups;

public sealed class RestoreBackupEndpoint(IMediator mediator)
    : Endpoint<RestoreBackupCommand, ApiResponse<RestoreBackupResult>>
{
    public override void Configure()
    {
        Post("/backups/restore");
        Options(x => x.WithTags("Backups"));
        Summary(s =>
        {
            s.Summary = "Restore a backup";
            s.Description = "Restores platform state from a filesystem snapshot or a git commit. Supports dry run to preview changes without applying them.";
            s[200] = "Restore completed (or dry run diff returned)";
            s[400] = "Validation error";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(RestoreBackupCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}