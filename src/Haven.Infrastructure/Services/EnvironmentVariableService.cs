using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Services;

public class EnvironmentVariableService(IServiceRepository serviceRepository, IEnvironmentVariableRepository environmentVariableRepository) : IEnvironmentVariableService
{
    public async Task<IEnumerable<EnvironmentVariables>> BuildVariablesForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null || service.Environment is null || service.Environment.Project is null)
        {
            return [];
        }
        
        var projectEnvs = await environmentVariableRepository.GetForProjectAsync(service.Environment.Project.Id, cancellationToken);
        var environmentEnvs =
            await environmentVariableRepository.GetForEnvironmentAsync(service.Environment.Id, cancellationToken);
        var serviceEnvs = await environmentVariableRepository.GetForServiceAsync(serviceId, cancellationToken);
        
        return Merge(projectEnvs, Merge(environmentEnvs, serviceEnvs));
    }
    
    
    private static IEnumerable<EnvironmentVariables> Merge(IEnumerable<EnvironmentVariables> @base,
        IEnumerable<EnvironmentVariables> overrides)
    {
        var dict = @base.ToDictionary(x => x.Key, x => x);

        foreach (var dest in overrides)
        {
            dict[dest.Key] = dest;
        }

        return dict.Values;
    }
}