using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.EnvironmentVariables.Queries.GetTemplateSuggestions;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.EnvironmentVariables;

public sealed class GetTemplateSuggestionsEndpoint(IMediator mediator)
    : Endpoint<GetTemplateSuggestionsQuery, ApiResponse<TemplateSuggestionsDto>>
{
    public override void Configure()
    {
        Get(
            "/projects/{projectId}/template-suggestions",
            "/projects/{projectId}/environments/{environmentId}/template-suggestions",
            "/projects/{projectId}/environments/{environmentId}/services/{serviceId}/template-suggestions");

        Options(x => x.WithTags("EnvironmentVariables"));
        Summary(s =>
        {
            s.Summary = "Get template reference suggestions";
            s.Description = "Returns the ${{ namespace.key }} references available when editing the variables of a " +
                            "project, environment or service. Only key names are returned, never values.";
            s[200] = "OK";
            s[404] = "Project, environment, or service not found";
        });
    }

    public override async Task HandleAsync(GetTemplateSuggestionsQuery req, CancellationToken ct)
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
