using FastEndpoints;

using Haven.Application.Common.Messaging;
using Haven.Application.Features.Secrets;
using Haven.Application.Features.Secrets.Queries.GetSecretVariablesForParent;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class GetProjectSecretsEndpoint(IMediator mediator) : Endpoint<GetSecretVariablesForParentQuery, PagedResult<SecretVariableDto>>
{
    public override void Configure()
    {
        Get("/projects/{projectId}/secrets");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "List project secrets";
            s.Description = "Returns a paginated list of secret variables scoped to a project.";
        });
    }

    public override async Task HandleAsync(GetSecretVariablesForParentQuery req, CancellationToken ct)
    {
        req.ParentId = Route<Guid>("projectId");
        req.ParentType = EnvironmentVariableParentType.Project;

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
