using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Configuration.Commands.UpdateConfiguration;
using Haven.Application.Features.Configuration.Dtos;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Configuration;

public sealed class UpdateConfigurationEndpoint(IMediator mediator)
    : Endpoint<UpdateConfigurationCommand, ApiResponse<HavenConfigurationDto>>
{
    public override void Configure()
    {
        Put("/configuration");

        Options(x => x.WithTags("Configuration"));
        Summary(s =>
        {
            s.Summary = "Update Haven configuration";
            s[200] = "Configuration updated";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(UpdateConfigurationCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}