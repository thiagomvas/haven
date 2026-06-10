using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Backups.Commands.CreateBackup;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Backups;

public class CreateBackupEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<CreateBackupResult>>
{
    public override void Configure()
    {
        Post("/backups");
        Options(x => x.WithTags("Backups"));
        Summary(s =>
        {
            s.Summary = "Create a backup";
            s.Description = "Serializes the full platform state to a versioned snapshot directory.";
            s[201] = "Backup created";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new CreateBackupCommand(), ct);
        await this.SendResultAsync(result, ct);
    }
}
