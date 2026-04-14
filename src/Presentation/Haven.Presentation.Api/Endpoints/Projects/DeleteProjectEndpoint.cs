using FastEndpoints;
using Haven.Application.Common;
using Haven.Application.Features.Projects.Commands.DeleteProject;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Projects;

public sealed class DeleteProjectEndpoint(IMediator mediator) : Endpoint<DeleteProjectCommand>
{
    public override void Configure()
    {
        Delete("/projects/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteProjectCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);

        if (result.IsSuccess)
            await Send.NoContentAsync(ct);
        else if (result.Error.Code == Error.NotFound.Code)
            await Send.NotFoundAsync(ct);
        else
            await Send.ResponseAsync(result.Error.Message, StatusCodes.Status400BadRequest, cancellation: ct);
    }
}
