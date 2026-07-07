using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services;
using Haven.Application.Features.Services.Queries.ListVolumesForService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class ListVolumesForServiceEndpoint(IMediator mediator)
    : Endpoint<ListVolumesForServiceQuery, ApiResponse<IReadOnlyList<ServiceVolumeDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "List volumes";
            s.Description = "Returns all volumes configured for a service.";
            s[200] = "OK";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(ListVolumesForServiceQuery req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
