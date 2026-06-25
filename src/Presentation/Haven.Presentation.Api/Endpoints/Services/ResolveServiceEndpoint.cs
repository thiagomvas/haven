using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Queries.ResolveService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public sealed class ResolveServiceEndpoint(IMediator mediator)
    : Endpoint<ResolveServiceQuery, ApiResponse<ServiceLocationDto>>
{
    public override void Configure()
    {
        Get("/services/{serviceId}");

        Options(x => x.WithTags("Services"));
        Summary(s =>
        {
            s.Summary = "Resolve service location";
            s.Description = "Returns the project and environment IDs for a service given only its ID.";
            s[200] = "OK";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(ResolveServiceQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}