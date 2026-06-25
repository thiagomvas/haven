using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Manifests.Commands.SyncFromManifests;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Manifests;

public sealed class SyncFromManifestsEndpoint(IMediator mediator) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/manifests/sync");

        Options(x => x.WithTags("Manifests"));
        Summary(s =>
        {
            s.Summary = "Sync database from manifests";
            s.Description = "Destructively restores DB state from the manifests directory. WARNING: This is irreversible and will delete all existing projects, environments, services, and networks, then re-populate from manifest files.";
            s[200] = "Synchronization completed successfully";
            s[500] = "Synchronization failed";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new SyncFromManifestsCommand(), ct);
        await this.SendResultAsync(result, ct);
    }
}