using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.WriteVolumeFileContent;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class WriteVolumeFileContentEndpoint(IMediator mediator)
    : Endpoint<WriteVolumeFileContentCommand, ApiResponse>
{
    public override void Configure()
    {
        Put("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes/{volumeId}/files/content");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Write a managed volume file";
            s.Description = "Creates or overwrites a file within a managed volume. Path and content are provided in the body.";
            s[200] = "Saved";
            s[400] = "Invalid path or volume is not managed";
            s[404] = "Service or volume not found";
        });
    }

    public override async Task HandleAsync(WriteVolumeFileContentCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.VolumeId = Route<Guid>("volumeId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
