using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries.GetServiceDependants;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class GetServiceDependantsEndpoint(IMediator mediator)
    : Endpoint<GetServiceDependantsQuery, ApiResponse<IReadOnlyList<ServiceDependantDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/dependants");

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Get service dependants";
            s.Description = "Returns the services in the same environment that reference this service.";
            s[200] = "OK";
            s[404] = "Environment or service not found";
        });
    }

    public override async Task HandleAsync(GetServiceDependantsQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
