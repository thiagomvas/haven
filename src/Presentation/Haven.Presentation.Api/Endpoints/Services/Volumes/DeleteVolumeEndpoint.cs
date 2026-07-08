using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.DeleteVolume;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class DeleteVolumeEndpoint(IMediator mediator)
    : Endpoint<DeleteVolumeCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes/{volumeId}");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Delete a volume";
            s.Description = "Removes a volume from a service. Managed volumes also have their files deleted.";
            s[200] = "Deleted";
            s[404] = "Service or volume not found";
        });
    }

    public override async Task HandleAsync(DeleteVolumeCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.VolumeId = Route<Guid>("volumeId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}