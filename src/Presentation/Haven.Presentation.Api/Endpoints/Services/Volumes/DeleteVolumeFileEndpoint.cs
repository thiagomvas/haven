using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.DeleteVolumeFile;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class DeleteVolumeFileEndpoint(IMediator mediator)
    : Endpoint<DeleteVolumeFileCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes/{volumeId}/files/content");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Delete a managed volume file";
            s.Description = "Deletes a file or subdirectory within a managed volume. The path is passed as the 'path' query parameter.";
            s[200] = "Deleted";
            s[400] = "Invalid path or volume is not managed";
            s[404] = "Service, volume, or file not found";
        });
    }

    public override async Task HandleAsync(DeleteVolumeFileCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.VolumeId = Route<Guid>("volumeId");
        req.Path = Query<string>("path", isRequired: false) ?? string.Empty;

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
