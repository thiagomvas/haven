using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Common.Templating;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.EnvironmentVariables.Queries.GetTemplateSuggestions;

public sealed class GetTemplateSuggestionsHandler(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    IServiceRepository serviceRepository,
    ISecretVariableRepository secretVariableRepository,
    IEnvironmentVariableService environmentVariableService,
    IFeatureFlagService featureFlagService)
    : IQueryHandler<GetTemplateSuggestionsQuery, TemplateSuggestionsDto>
{
    public async ValueTask<Result<TemplateSuggestionsDto>> Handle(GetTemplateSuggestionsQuery query,
        CancellationToken cancellationToken)
    {
        return query.ParentType switch
        {
            EnvironmentVariableParentType.Project => await ForProject(query.ParentId, cancellationToken),
            EnvironmentVariableParentType.Environment => await ForEnvironment(query.ParentId, cancellationToken),
            EnvironmentVariableParentType.Service => await ForService(query.ParentId, cancellationToken),
            _ => Error.Validation($"Unsupported parent type '{query.ParentType}'.")
        };
    }

    private async Task<Result<TemplateSuggestionsDto>> ForProject(Guid projectId, CancellationToken ct)
    {
        if (await projectRepository.GetByIdAsync(projectId, ct) is null)
            return Error.NotFoundFor("Project", projectId);

        var variables = new List<TemplateSuggestionDto>();
        variables.AddRange(Keys(TemplateNamespaces.EnvVariables,
            (await environmentVariableService.BuildVariablesForProjectAsync(projectId, ct)).Select(e => e.Key)));
        variables.AddRange(Keys(TemplateNamespaces.Secrets,
            await SecretKeysAsync(projectId, EnvironmentVariableParentType.Project, ct)));

        return Build(variables, isExhaustive: false, includeServiceScoped: false);
    }

    private async Task<Result<TemplateSuggestionsDto>> ForEnvironment(Guid environmentId, CancellationToken ct)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, ct);
        if (environment is null)
            return Error.NotFoundFor("Environment", environmentId);

        var envKeys = (await environmentVariableService.BuildVariablesForEnvironmentAsync(environmentId, ct))
            .Select(e => e.Key);
        var secretKeys = (await SecretKeysAsync(environment.ProjectId, EnvironmentVariableParentType.Project, ct))
            .Concat(await SecretKeysAsync(environmentId, EnvironmentVariableParentType.Environment, ct));

        var variables = new List<TemplateSuggestionDto>();
        variables.AddRange(Keys(TemplateNamespaces.EnvVariables, envKeys));
        variables.AddRange(Keys(TemplateNamespaces.Secrets, secretKeys));

        return Build(variables, isExhaustive: false, includeServiceScoped: false);
    }

    private async Task<Result<TemplateSuggestionsDto>> ForService(Guid serviceId, CancellationToken ct)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, ct);
        if (service is null)
            return Error.NotFoundFor("Service", serviceId);

        var environment = await environmentRepository.GetByIdAsync(service.EnvironmentId, ct);
        if (environment is null)
            return Error.NotFoundFor("Environment", service.EnvironmentId);

        // Secrets from the project and environment are shared by every service in the environment.
        var inheritedSecretKeys = (await SecretKeysAsync(environment.ProjectId, EnvironmentVariableParentType.Project, ct))
            .Concat(await SecretKeysAsync(environment.Id, EnvironmentVariableParentType.Environment, ct))
            .ToList();

        var variables = new List<TemplateSuggestionDto>();
        variables.AddRange(Keys(TemplateNamespaces.EnvVariables, await EnvKeysAsync(serviceId, ct)));
        variables.AddRange(Keys(TemplateNamespaces.Secrets,
            inheritedSecretKeys.Concat(await SecretKeysAsync(serviceId, EnvironmentVariableParentType.Service, ct))));
        variables.AddRange(Keys(TemplateNamespaces.Outputs, service.ComputedProperties.Select(p => p.Key)));

        // Only the other services in the same environment are addressable, by alias.
        var siblings = await serviceRepository.GetByEnvironmentIdAsync(service.EnvironmentId, ct);
        foreach (var sibling in siblings)
        {
            if (sibling.Id == service.Id || string.IsNullOrWhiteSpace(sibling.Alias))
                continue;

            var siblingKeys = new Dictionary<string, IEnumerable<string>>
            {
                [TemplateNamespaces.EnvVariables] = await EnvKeysAsync(sibling.Id, ct),
                [TemplateNamespaces.Secrets] = inheritedSecretKeys
                    .Concat(await SecretKeysAsync(sibling.Id, EnvironmentVariableParentType.Service, ct)),
                [TemplateNamespaces.Outputs] = sibling.ComputedProperties.Select(p => p.Key),
                [TemplateNamespaces.Runtime] = TemplateNamespaces.RuntimeKeys.Keys,
            };

            foreach (var (scope, keys) in siblingKeys)
                variables.AddRange(Keys(TemplateNamespaces.Services, keys.Select(k => $"{sibling.Alias}.{scope}.{k}")));
        }

        return Build(variables, isExhaustive: true, includeServiceScoped: true);
    }

    private async Task<IEnumerable<string>> EnvKeysAsync(Guid serviceId, CancellationToken ct)
    {
        var envs = await environmentVariableService.BuildVariablesForServiceAsync(serviceId, ct);
        var flags = await featureFlagService.GetFlagsAsEnvironmentsForServiceAsync(serviceId, ct);
        return envs.Select(e => e.Key).Concat(flags.Select(f => f.Key));
    }

    private async Task<IEnumerable<string>> SecretKeysAsync(Guid parentId, EnvironmentVariableParentType parentType,
        CancellationToken ct) =>
        (await secretVariableRepository.GetForParentAsync(parentId, parentType, ct)).Select(s => s.Key);

    private static IEnumerable<TemplateSuggestionDto> Keys(string @namespace, IEnumerable<string> keys) =>
        keys.Distinct().Order().Select(k => new TemplateSuggestionDto { Namespace = @namespace, Key = k });

    private static Result<TemplateSuggestionsDto> Build(List<TemplateSuggestionDto> variables, bool isExhaustive,
        bool includeServiceScoped)
    {
        variables.AddRange(TemplateNamespaces.RuntimeKeys.Select(kv => new TemplateSuggestionDto
        {
            Namespace = TemplateNamespaces.Runtime,
            Key = kv.Key,
            Description = kv.Value
        }));

        string[] namespaces = includeServiceScoped
            ?
            [
                TemplateNamespaces.EnvVariables, TemplateNamespaces.Secrets, TemplateNamespaces.Runtime,
                TemplateNamespaces.Outputs, TemplateNamespaces.Services
            ]
            : [TemplateNamespaces.EnvVariables, TemplateNamespaces.Secrets, TemplateNamespaces.Runtime];

        return Result<TemplateSuggestionsDto>.Success(new TemplateSuggestionsDto
        {
            Namespaces = namespaces,
            Variables = variables,
            Filters = TemplateExpressionResolver.DefaultFilters.Keys.ToList(),
            IsExhaustive = isExhaustive
        });
    }
}
