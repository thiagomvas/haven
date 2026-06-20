using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.Backups.Queries.GetBackupOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Backups;

public sealed class GetBackupOptionsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<BackupOptions>>
{
    public override void Configure()
    {
        Get("/backups/options");
        Options(x => x.WithTags("Backups"));
        Summary(s =>
        {
            s.Summary = "Get backup options";
            s.Description = "Returns the current backup configuration.";
            s[200] = "Success";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetBackupOptionsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}