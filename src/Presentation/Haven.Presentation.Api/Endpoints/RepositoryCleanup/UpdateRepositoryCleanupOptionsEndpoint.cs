using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;
using Haven.Application.Features.RepositoryCleanup.Commands.UpdateRepositoryCleanupOptions;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.RepositoryCleanup;

public sealed class UpdateRepositoryCleanupOptionsEndpoint(IMediator mediator)
    : Endpoint<UpdateRepositoryCleanupOptionsCommand, ApiResponse<RepositoryCleanupOptions>>
{
    public override void Configure()
    {
        Put("/repository-cleanup/options");
        Options(x => x.WithTags("RepositoryCleanup"));
        Summary(s =>
        {
            s.Summary = "Update repository cleanup options";
            s.Description = "Persists the dangling repository cleanup job configuration to the database.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(UpdateRepositoryCleanupOptionsCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
