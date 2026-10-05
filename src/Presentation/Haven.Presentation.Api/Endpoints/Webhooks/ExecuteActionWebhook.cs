using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.CustomActions.Commands.ExecuteCustomAction;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Webhooks;

public class ExecuteActionWebhook(IMediator mediator) : EndpointWithoutRequest<ApiResponse>
{
    public override void Configure()
    {
        Post("/webhooks/action/{Token}");
        AllowAnonymous();
        RoutePrefixOverride(string.Empty);
        Summary(s =>
        {
            s.Summary = "Execute an action via webhook";
            s.Description = "This endpoint allows you to execute an action via a webhook. The action is identified by the token provided in the URL.";
            s.Response<ApiResponse>(200, "Action executed successfully");
            s.Response<ApiResponse>(400, "Invalid request");
            s.Response<ApiResponse>(401, "Unauthorized");
            s.Response<ApiResponse>(404, "Action not found");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var token = Route<string>("Token");
        if (string.IsNullOrWhiteSpace(token))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var result = await mediator.Send(new ExecuteCustomActionCommand { Token = token }, ct);
        await this.SendResultAsync(result, ct);
    }
}