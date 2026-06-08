using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Instance.Dtos;
using Haven.Application.Features.Instance.Queries.GetInstance;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class GetInstanceEndpoint(IMediator mediator)
    : Endpoint<EmptyRequest, ApiResponse<InstanceDto>>
{
    public override void Configure()
    {
        Get("/configuration/instance");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Get instance configuration";
            s[200] = "Instance configuration retrieved";
        });
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInstanceQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
