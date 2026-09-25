using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.Secrets;
using Haven.Application.Features.Secrets.Queries.GetSecretVariablesForParent;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class GetSecretsEndpoint(IMediator mediator) : Endpoint<GetSecretVariablesForParentQuery, PagedResult<SecretVariableDto>>
{
    public override void Configure()
    {
        Get(
            "/projects/{projectId}/secrets",
            "/projects/{projectId}/environments/{environmentId}/secrets",
            "/projects/{projectId}/environments/{environmentId}/services/{serviceId}/secrets");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "List secrets";
            s.Description = "Returns a paginated list of secret variables scoped to a project, environment or service.";
        });
    }

    public override async Task HandleAsync(GetSecretVariablesForParentQuery req, CancellationToken ct)
    {
        var serviceId = Route<Guid?>("serviceId", isRequired: false);
        var environmentId = Route<Guid?>("environmentId", isRequired: false);
        var projectId = Route<Guid?>("projectId", isRequired: false);

        (req.ParentId, req.ParentType) = (serviceId, environmentId, projectId) switch
        {
            ({ } id, _, _) => (id, EnvironmentVariableParentType.Service),
            (_, { } id, _) => (id, EnvironmentVariableParentType.Environment),
            (_, _, { } id) => (id, EnvironmentVariableParentType.Project),
            _ => throw new InvalidOperationException("No parent route parameter was matched.")
        };

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
