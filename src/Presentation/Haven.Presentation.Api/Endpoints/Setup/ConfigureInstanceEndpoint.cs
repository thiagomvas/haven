using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Setup.Commands.ConfigureInstanceCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Setup;

public sealed class ConfigureInstanceEndpoint(IMediator mediator) : Endpoint<ConfigureInstanceCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/setup/instance");
        
        Options(x => x.WithTags("Setup"));
        Summary(s =>
        {
            s.Summary = "Configure instance";
            s.Description = "Sets the instance name and timezone. This is the first step of setup.";
            s[200] = "Instance configured";
            s[400] = "Validation error";
            s[409] = "Instance already configured";
        });
    }

    public override async Task HandleAsync(ConfigureInstanceCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}