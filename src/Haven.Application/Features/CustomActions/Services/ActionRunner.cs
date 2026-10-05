using Haven.Application.Common;
using Haven.Application.Common.Templating;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.CustomActions.Services;

public sealed class ActionRunner(
    IEnumerable<IActionStrategy> strategies,
    ICustomActionRepository repository,
    IServiceTemplateNamespaceProvider namespaceProvider,
    ILogger<ActionRunner> logger) : IActionRunner
{
    public async Task<Result> RunActionAsync(Guid actionId, IReadOnlyDictionary<string, string>? inputs,
        CancellationToken ct)
    {
        var action = await repository.GetByIdAsync(actionId, ct);
        if (action is null) return Error.NotFoundFor(nameof(CustomAction), actionId);
        var strategy = strategies.FirstOrDefault(s => s.CanHandle(action));
        if (strategy is null)
        {
            logger.LogWarning("No strategy found for action: {ActionId}", actionId);
            return Error.NotFoundFor(nameof(IActionStrategy), actionId);
        }

        var inputValues = ResolveInputs(action.Inputs, inputs);
        if (inputValues.IsFailure) return inputValues.Error;

        var namespaces = await namespaceProvider.BuildAsync(action.ServiceId, ct);
        var allNamespaces = new Dictionary<string, TemplateNamespaceResolver>(
            namespaces ?? new Dictionary<string, TemplateNamespaceResolver>())
        {
            ["inputs"] = key => inputValues.Value.GetValueOrDefault(key)
        };
        var resolved = action.WithConfig(ResolveConfig(action.Config, allNamespaces));

        return await strategy.ExecuteAsync(resolved, ct);
    }

    /// <summary>
    /// Merges caller-supplied values over the declared defaults, rejecting undeclared keys and missing required values.
    /// </summary>
    private static Result<IReadOnlyDictionary<string, string>> ResolveInputs(
        CustomActionInput[] declared, IReadOnlyDictionary<string, string>? supplied)
    {
        supplied ??= new Dictionary<string, string>();

        var unknown = supplied.Keys.FirstOrDefault(k => declared.All(d => d.Name != k));
        if (unknown is not null)
            return Error.Validation($"Unknown input '{unknown}'.");

        var values = new Dictionary<string, string>();
        foreach (var input in declared)
        {
            if (supplied.TryGetValue(input.Name, out var value) && !string.IsNullOrEmpty(value))
                values[input.Name] = value;
            else if (input.DefaultValue is not null)
                values[input.Name] = input.DefaultValue;
            else if (input.Required)
                return Error.Validation($"Input '{input.Name}' is required.");
        }

        return values;
    }

    private static ActionConfig ResolveConfig(ActionConfig config,
        IReadOnlyDictionary<string, TemplateNamespaceResolver> namespaces)
    {
        string Resolve(string value) => TemplateExpressionResolver.Resolve(value, namespaces);

        return config switch
        {
            ExecActionConfig exec => exec with
            {
                Command = exec.Command.Select(Resolve).ToList(),
                WorkingDir = exec.WorkingDir is null ? null : Resolve(exec.WorkingDir),
                User = exec.User is null ? null : Resolve(exec.User)
            },
            HttpActionConfig http => http with
            {
                Url = Resolve(http.Url),
                Headers = http.Headers.ToDictionary(h => h.Key, h => Resolve(h.Value)),
                Body = http.Body is null ? null : Resolve(http.Body)
            },
            _ => config
        };
    }
}