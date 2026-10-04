using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.CustomActions.Commands.CreateCustomAction;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.CustomActions;

public sealed class CreateCustomActionEndpoint(IMediator mediator)
    : Endpoint<CreateCustomActionCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/actions");

        Options(x => x.WithTags("Custom Actions"));
        Summary(s =>
        {
            s.Summary = "Create a custom action";
            s.Description = "Creates a custom action for a service and returns its ID.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Service not found";
            s[409] = "Action name conflict";
        });
    }

    public override async Task HandleAsync(CreateCustomActionCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
