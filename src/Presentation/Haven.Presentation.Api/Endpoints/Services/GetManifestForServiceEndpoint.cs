using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries.GetManifestForService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public class GetManifestForServiceEndpoint(IMediator mediator) : Endpoint<GetManifestForServiceQuery, ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/projects/{projectId:guid}/environments/{environmentId:guid}/services/{serviceId:guid}/manifest");
    }
    
    public override async Task HandleAsync(GetManifestForServiceQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}