using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Sidecars;
using Haven.Application.Features.Sidecars.Queries.ListSidecars;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Sidecars;

public sealed class ListSidecarsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<IReadOnlyList<SidecarDto>>>
{
    public override void Configure()
    {
        Get("/sidecars");

        Options(x => x.WithTags("Sidecars"));
        Summary(s =>
        {
            s.Summary = "List sidecars";
            s.Description = "Returns all sidecars in Haven's built-in catalog, with their current status and enabled state.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new ListSidecarsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}