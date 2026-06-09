using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries;
using Haven.Application.Features.Services.Queries.GetService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class GetServiceEndpoint(IMediator mediator)
    : Endpoint<GetServiceQuery, ApiResponse<ServiceDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}");
        AllowAnonymous();
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Get service";
            s.Description = "Returns a service by ID.";
            s[200] = "OK";
            s[404] = "Project, environment, or service not found";
        });
    }

    public override async Task HandleAsync(GetServiceQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}