using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions.Abstractions;

public interface ICustomActionRepository
{
    Task<CustomAction?> GetByIdAsync(Guid actionId, CancellationToken ct);
    Task<CustomAction?> GetByTokenAsync(string token, CancellationToken ct);
    Task<IReadOnlyList<CustomAction>> GetForServiceAsync(Guid serviceId, CancellationToken ct);
    Task AddAsync(CustomAction action, CancellationToken ct);
    Task RemoveAsync(CustomAction action, CancellationToken ct);
}