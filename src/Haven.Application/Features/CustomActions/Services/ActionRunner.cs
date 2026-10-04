using Haven.Application.Common;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.CustomActions.Services;

public sealed class ActionRunner(IEnumerable<IActionStrategy> strategies, ICustomActionRepository repository, ILogger<ActionRunner> logger) : IActionRunner
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
        return await strategy.ExecuteAsync(action, ct);
    }
}