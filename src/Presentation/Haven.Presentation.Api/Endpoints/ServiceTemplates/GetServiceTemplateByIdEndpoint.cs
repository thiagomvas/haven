using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceTemplates.Queries.GetServiceTemplateById;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceTemplates;

public sealed class GetServiceTemplateByIdEndpoint(IMediator mediator)
    : Endpoint<GetServiceTemplateByIdQuery, ApiResponse<ServiceTemplateDto>>
{
    public override void Configure()
    {
        Get("/service-templates/{id}");
        Options(x => x.WithTags("Service Templates"));
        Summary(s =>
        {
            s.Summary = "Get service template";
            s.Description = "Returns a built-in service template by ID, including its configurable inputs.";
            s[200] = "OK";
            s[404] = "Template not found";
        });
    }

    public override async Task HandleAsync(GetServiceTemplateByIdQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
