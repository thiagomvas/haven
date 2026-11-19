using FastEndpoints;

using Haven.Application.Common;
using Haven.Application.Features.Services.Commands.DeployServiceViaWebhook;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Webhooks;

public class DeployServiceWebhook(IMediator mediator) : EndpointWithoutRequest<Result>
{
    public override void Configure()
    {
        Post("/webhooks/deploy/{Token}");

        Options(x => x.WithTags("Webhooks"));
        Summary(s =>
        {
            s.Summary = "Deploy a service via webhook";
            s.Description = "Triggers a deployment of the service identified by the webhook token in the URL. The token acts as the credential, so no other authentication is required.";
            s[200] = "Deployment triggered";
            s[401] = "Invalid token";
            s[404] = "Service not found";
        });
        RoutePrefixOverride(string.Empty);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var token = Route<string>("Token")!;
        var result = await mediator.Send(new DeployServiceViaWebhookCommand { Token = token }, ct);
        await this.SendResultAsync(result, ct);
    }
}