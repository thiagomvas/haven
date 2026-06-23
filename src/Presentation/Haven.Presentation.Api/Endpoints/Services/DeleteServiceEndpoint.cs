using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.DeleteService;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services;

public class DeleteServiceEndpoint(IMediator mediator) : Endpoint<DeleteServiceCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("projects/{projectId:guid}/environments/{environmentId:guid}/services/{serviceId:guid}",
            "services/{serviceId:guid}");
        Summary(s =>
        {
            s.Summary = "Delete a service";
            s.Description = "Deletes a service from the system.";
            s[200] = "Success";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(DeleteServiceCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}