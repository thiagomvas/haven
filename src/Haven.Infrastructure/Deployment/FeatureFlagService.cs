using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Deployment;

public class FeatureFlagService(IFeatureFlagRepository repository) : IFeatureFlagService
{
    public async Task<IReadOnlyList<EnvironmentVariables>> GetFlagsAsEnvironmentsForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        var flags = await repository.GetForServiceListAsync(serviceId, ct);
        return flags.Select(f => new EnvironmentVariables()
        {
            Key = f.Key,
            Value = f.Value,
            ParentId = serviceId,
            ParentType = EnvironmentVariableParentType.Service
        }).ToList();
    }
}