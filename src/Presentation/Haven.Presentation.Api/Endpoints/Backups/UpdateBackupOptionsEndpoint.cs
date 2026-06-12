using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.Backups.Commands.UpdateBackupOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Backups;

public sealed class UpdateBackupOptionsEndpoint(IMediator mediator)
    : Endpoint<UpdateBackupOptionsCommand, ApiResponse<BackupOptions>>
{
    public override void Configure()
    {
        Put("/backups/options");
        Options(x => x.WithTags("Backups"));
        Summary(s =>
        {
            s.Summary = "Update backup options";
            s.Description = "Persists backup configuration to the database.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(UpdateBackupOptionsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}