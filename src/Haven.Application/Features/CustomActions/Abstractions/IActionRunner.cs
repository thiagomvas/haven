using Haven.Application.Common;

namespace Haven.Application.Features.CustomActions.Abstractions;

public interface IActionRunner
{
    /// <summary>
    /// Runs the custom action with the given identifier.
    /// </summary>
    /// <param name="actionId">The identifier of the action to run.</param>
    /// <param name="inputs">Values for the action's declared inputs, referenced as <c>${{ inputs.name }}</c>.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Result> RunActionAsync(Guid actionId, IReadOnlyDictionary<string, string>? inputs, CancellationToken ct);
}