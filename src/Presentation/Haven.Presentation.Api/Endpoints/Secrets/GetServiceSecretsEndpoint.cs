using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.Secrets;
using Haven.Application.Features.Secrets.Queries.GetSecretVariablesForParent;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class GetServiceSecretsEndpoint(IMediator mediator) : Endpoint<GetSecretVariablesForParentQuery, PagedResult<SecretVariableDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/environments/{environmentId}/services/{serviceId}/secrets");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "List service secrets";
            s.Description = "Returns a paginated list of secret variables scoped to a service.";
        });
    }

    public override async Task HandleAsync(GetSecretVariablesForParentQuery req, CancellationToken ct)
    {
        req.ParentId = Route<Guid>("serviceId");
        req.ParentType = EnvironmentVariableParentType.Service;

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
