using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries;
using Haven.Application.Features.Services.Queries.GetServicesByEnvironment;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class GetServicesByEnvironmentEndpoint(IMediator mediator)
    : Endpoint<GetServicesByEnvironmentQuery, ApiResponse<IReadOnlyList<ServiceDto>>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services");
        
        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "List services";
            s.Description = "Returns all services belonging to an environment.";
            s[200] = "OK";
            s[404] = "Environment not found";
        });
    }

    public override async Task HandleAsync(GetServicesByEnvironmentQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}