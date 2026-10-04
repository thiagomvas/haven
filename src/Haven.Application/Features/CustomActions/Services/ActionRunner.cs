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
    public async Task<Result> RunActionAsync(Guid actionId, CancellationToken ct)
    {
        var action = await repository.GetByIdAsync(actionId, ct);
        if (action is null) return Error.NotFoundFor(nameof(CustomAction), actionId);
        var strategy = strategies.FirstOrDefault(s => s.CanHandle(action));
        if (strategy is null)
        {
            logger.LogWarning("No strategy found for action: {ActionId}", actionId);
            return Error.NotFoundFor(nameof(IActionStrategy), actionId);
        }

        var namespaces = await namespaceProvider.BuildAsync(action.ServiceId, ct);
        var resolved = namespaces is null ? action : action.WithConfig(ResolveConfig(action.Config, namespaces));

        return await strategy.ExecuteAsync(resolved, ct);
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
