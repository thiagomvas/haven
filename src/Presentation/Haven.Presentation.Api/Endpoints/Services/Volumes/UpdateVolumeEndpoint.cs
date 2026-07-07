using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.UpdateVolume;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class UpdateVolumeEndpoint(IMediator mediator)
    : Endpoint<UpdateVolumeCommand, ApiResponse>
{
    public override void Configure()
    {
        Patch("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes/{volumeId}");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Update a volume";
            s.Description = "Partially updates a volume's configuration (target, source, read-only, backup flag).";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Service or volume not found";
        });
    }

    public override async Task HandleAsync(UpdateVolumeCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.VolumeId = Route<Guid>("volumeId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
