using FastEndpoints;

using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries.GetVolumeFiles;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class GetVolumeFilesEndpoint(IMediator mediator)
    : Endpoint<GetVolumeFilesQuery, ApiResponse<IReadOnlyList<ManagedVolumeFileEntry>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes/{volumeId}/files");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "List managed volume files";
            s.Description = "Returns the file tree of a managed volume.";
            s[200] = "OK";
            s[400] = "Volume is not a managed volume";
            s[404] = "Service or volume not found";
        });
    }

    public override async Task HandleAsync(GetVolumeFilesQuery req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.VolumeId = Route<Guid>("volumeId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
