using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class DeleteDomainEndpoint(IMediator mediator)
    : Endpoint<DeleteDomainCommand, ApiResponse>
{
    public override void Configure()
    {
        Delete("/service-registry/{serviceId}/domains/{domainId}");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Delete a custom domain";
            s.Description = "Removes a registered domain from a service's service registry entry.";
            s[200] = "Deleted";
            s[404] = "Service registry entry or domain not found";
        });
    }

    public override async Task HandleAsync(DeleteDomainCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");
        req.DomainId = Route<Guid>("domainId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
