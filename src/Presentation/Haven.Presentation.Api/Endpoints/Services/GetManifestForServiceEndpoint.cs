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

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Get service manifest";
            s.Description = "Returns the YAML manifest of the service.";
            s[200] = "Manifest YAML";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(GetManifestForServiceQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}