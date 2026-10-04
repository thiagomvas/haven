using Haven.Application.Common;

namespace Haven.Application.Features.CustomActions.Abstractions;

public interface IActionRunner
{
    Task<Result> RunActionAsync(Guid actionId, CancellationToken ct);
}