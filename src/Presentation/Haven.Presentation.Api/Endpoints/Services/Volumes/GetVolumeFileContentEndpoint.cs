using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries.GetVolumeFileContent;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class GetVolumeFileContentEndpoint(IMediator mediator)
    : Endpoint<GetVolumeFileContentQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes/{volumeId}/files/content");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Read a managed volume file";
            s.Description = "Returns the text content of a file within a managed volume. The file path is passed as the 'path' query parameter.";
            s[200] = "OK";
            s[400] = "Invalid path or volume is not managed";
            s[404] = "Service, volume, or file not found";
        });
    }

    public override async Task HandleAsync(GetVolumeFileContentQuery req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.VolumeId = Route<Guid>("volumeId");
        req.Path = Query<string>("path", isRequired: false) ?? string.Empty;

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
