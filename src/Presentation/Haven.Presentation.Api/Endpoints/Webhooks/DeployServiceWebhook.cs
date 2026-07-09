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
        AllowAnonymous();
        RoutePrefixOverride(string.Empty);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var token = Route<string>("Token")!;
        var result = await mediator.Send(new DeployServiceViaWebhookCommand { Token = token }, ct);
        await this.SendResultAsync(result, ct);
    }
}