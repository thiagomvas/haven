using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.ServiceRegistry.Commands.AddDomain;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.ServiceRegistry.Domains;

public sealed class AddDomainEndpoint(IMediator mediator)
    : Endpoint<AddDomainCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/service-registry/{serviceId}/domains");

        Options(x => x.WithTags("ServiceRegistry"));
        Summary(s =>
        {
            s.Summary = "Add a custom domain";
            s.Description = "Registers a custom hostname (and target container port) for a service in the service registry. Creates a registry entry for the service if one doesn't exist yet.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[404] = "Service not found";
            s[409] = "A domain with that hostname already exists";
        });
    }

    public override async Task HandleAsync(AddDomainCommand req, CancellationToken ct)
    {
        req.ServiceId = Route<Guid>("serviceId");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
