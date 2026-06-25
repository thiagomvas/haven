using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Backups.Queries.ListSnapshots;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Backups;

public sealed class ListSnapshotsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<IReadOnlyList<SnapshotInfo>>>
{
    public override void Configure()
    {
        Get("/backups/snapshots");
        Options(x => x.WithTags("Backups"));
        Summary(s =>
        {
            s.Summary = "List backup snapshots";
            s.Description = "Returns all available filesystem backup snapshots sorted newest first.";
            s[200] = "Success";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new ListSnapshotsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
