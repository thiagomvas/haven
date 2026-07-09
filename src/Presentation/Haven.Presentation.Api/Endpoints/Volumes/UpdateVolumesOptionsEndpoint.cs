using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.Volumes.Commands.UpdateVolumesOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Volumes;

public sealed class UpdateVolumesOptionsEndpoint(IMediator mediator)
    : Endpoint<UpdateVolumesOptionsCommand, ApiResponse<VolumesOptions>>
{
    public override void Configure()
    {
        Put("/volumes/options");
        Options(x => x.WithTags("Volumes"));
        Summary(s =>
        {
            s.Summary = "Update volumes options";
            s.Description = "Persists managed-volumes configuration to the database.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(UpdateVolumesOptionsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}