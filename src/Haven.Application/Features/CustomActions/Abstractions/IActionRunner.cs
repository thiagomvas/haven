using Haven.Application.Common;

namespace Haven.Application.Features.CustomActions.Abstractions;

public interface IActionRunner
{
    /// <param name="inputs">Values for the action's declared inputs, referenced as <c>${{ inputs.name }}</c>.</param>
    Task<Result> RunActionAsync(Guid actionId, IReadOnlyDictionary<string, string>? inputs, CancellationToken ct);
}