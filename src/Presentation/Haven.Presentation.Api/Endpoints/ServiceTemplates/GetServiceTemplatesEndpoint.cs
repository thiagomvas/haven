using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplates;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceTemplates;

public sealed class GetServiceTemplatesEndpoint(IMediator mediator)
    : Endpoint<GetServiceTemplatesQuery, ApiResponse<IReadOnlyList<ServiceTemplateSummaryDto>>>
{
    public override void Configure()
    {
        Get("/service-templates");
        Options(x => x.WithTags("Service Templates"));
        Summary(s =>
        {
            s.Summary = "List service templates";
            s.Description = "Returns the built-in service templates (e.g. Postgres, Redis) available for creating services.";
            s[200] = "OK";
        });
    }

    public override async Task HandleAsync(GetServiceTemplatesQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
