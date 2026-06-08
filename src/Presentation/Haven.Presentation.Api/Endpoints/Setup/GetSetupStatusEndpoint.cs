using FastEndpoints;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Setup.Queries.GetSetupStatusQuery;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Setup;

public sealed class GetSetupStatusEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<GetSetupStatusResult>>
{
    public override void Configure()
    {
        Get("/setup/status");
        AllowAnonymous();
        Options(x => x.WithTags("Setup"));
        Summary(s =>
        {
            s.Summary = "Get setup status";
            s.Description = "Returns the current setup stage. Used to resume a partially completed setup wizard.";
            s[200] = "Current setup stage";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new GetSetupStatusQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
