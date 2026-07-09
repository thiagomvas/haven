using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.Volumes.Queries.GetVolumesOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Volumes;

public sealed class GetVolumesOptionsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<VolumesOptions>>
{
    public override void Configure()
    {
        Get("/volumes/options");
        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Get volumes options";
            s.Description = "Returns the current managed-volumes configuration.";
            s[200] = "Success";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetVolumesOptionsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}