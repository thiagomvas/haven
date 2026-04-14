using FastEndpoints;
using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Projects.Commands.UpdateProject;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class UpdateProjectEndpoint(IMediator mediator) : Endpoint<UpdateProjectCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/projects/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateProjectCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        var response = ApiResponse<Guid>.FromResult(result);

        if (result.IsSuccess)
            await Send.OkAsync(response, cancellation: ct);
        else if (result.Error.Code == Error.NotFound.Code)
            await Send.NotFoundAsync(ct);
        else if (result.Error.Code == Error.Conflict.Code)
            await Send.ResponseAsync(response, StatusCodes.Status409Conflict, cancellation: ct);
        else
            await Send.ResponseAsync(response, StatusCodes.Status400BadRequest, cancellation: ct);
    }
}
