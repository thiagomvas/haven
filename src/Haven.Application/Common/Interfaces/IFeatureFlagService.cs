using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces;

public interface IFeatureFlagService
{
    Task<IReadOnlyList<EnvironmentVariables>> GetFlagsAsEnvironmentsForServiceAsync(Guid serviceId,
        CancellationToken ct);
}