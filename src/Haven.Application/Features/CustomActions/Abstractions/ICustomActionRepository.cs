using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions.Abstractions;

public interface ICustomActionRepository
{
    Task<CustomAction?> GetByIdAsync(Guid actionId, CancellationToken ct);
    Task<IReadOnlyList<CustomAction>> GetForServiceAsync(Guid serviceId, CancellationToken ct);
}