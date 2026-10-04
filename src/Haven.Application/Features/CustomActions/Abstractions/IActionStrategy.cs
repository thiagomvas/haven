using Haven.Application.Common;
using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions.Abstractions;

public interface IActionStrategy
{
    bool CanHandle(CustomAction action);
    Task<Result> ExecuteAsync(CustomAction action, CancellationToken ct);
}