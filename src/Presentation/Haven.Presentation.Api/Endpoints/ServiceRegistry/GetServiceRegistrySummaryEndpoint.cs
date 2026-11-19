using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistrySummary;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry;

public sealed class GetServiceRegistrySummaryEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<ServiceRegistrySummaryDto>>
{
    public override void Configure()
    {
        Get("/service-registry/summary");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Summarize the service registry";
            s.Description = "Returns counts of registry entries by runtime state, plus total domains and externally exposed services.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetServiceRegistrySummaryQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}