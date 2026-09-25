using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Secrets.Commands.CreateSecretVariable;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Secrets;

public sealed class CreateSecretEndpoint(IMediator mediator) : Endpoint<CreateSecretVariableCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post(
            "/projects/{projectId}/secrets",
            "/projects/{projectId}/environments/{environmentId}/secrets",
            "/projects/{projectId}/environments/{environmentId}/services/{serviceId}/secrets");

        Options(x => x.WithTags("Secrets"));
        Summary(s =>
        {
            s.Summary = "Create a secret";
            s.Description = "Creates a new secret variable scoped to a project, environment or service.";
            s[201] = "Created";
            s[400] = "Validation error";
            s[409] = "A secret with this key already exists for the parent scope";
        });
    }

    public override async Task HandleAsync(CreateSecretVariableCommand req, CancellationToken ct)
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