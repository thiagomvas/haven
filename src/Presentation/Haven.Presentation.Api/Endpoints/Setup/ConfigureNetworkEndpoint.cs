using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Setup.Commands.ConfigureNetworkCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Setup;

public sealed class ConfigureNetworkEndpoint(IMediator mediator) : Endpoint<ConfigureNetworkCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/setup/network");
        
        Options(x => x.WithTags("Setup"));
        Summary(s =>
        {
            s.Summary = "Configure network access";
            s.Description = "Sets the host, port, and TLS settings. This is the final step of setup.";
            s[200] = "Network configured";
            s[400] = "Validation error";
            s[409] = "Setup already completed or super user not created yet";
        });
    }

    public override async Task HandleAsync(ConfigureNetworkCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}