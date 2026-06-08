using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Instance.Commands.UpdateInstance;
using Haven.Application.Features.Instance.Dtos;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class UpdateInstanceEndpoint(IMediator mediator)
    : Endpoint<UpdateInstanceCommand, ApiResponse<InstanceDto>>
{
    public override void Configure()
    {
        Put("/configuration/instance");
        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Update instance configuration";
            s[200] = "Instance configuration updated";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(UpdateInstanceCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
