using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class UpdateDomainEndpoint(IMediator mediator)
    : Endpoint<UpdateDomainCommand, ApiResponse>
{
    public override void Configure()
    {
        Patch("/service-registry/{serviceId}/domains/{domainId}");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Update a custom domain";
            s.Description = "Partially updates a service's registered domain (hostname and/or target container port).";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Service registry entry or domain not found";
            s[409] = "A domain with that hostname already exists";
        });
    }

    public override async Task HandleAsync(UpdateDomainCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
