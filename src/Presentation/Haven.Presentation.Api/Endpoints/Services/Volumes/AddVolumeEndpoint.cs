using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Services.Commands.AddVolume;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Services.Volumes;

public sealed class AddVolumeEndpoint(IMediator mediator)
    : Endpoint<AddVolumeCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/volumes");

        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Add a volume";
            s.Description = "Adds a named, host-path, or managed volume to a service and returns its ID.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Service not found";
        });
    }

    public override async Task HandleAsync(AddVolumeCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}