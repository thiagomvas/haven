using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Services.Graph;

namespace Haven.Application.Features.Services.Queries.GetServiceDependants;

public sealed class GetServiceDependantsHandler(
    IEnvironmentRepository environmentRepository,
    IEnvironmentVariableRepository environmentVariableRepository,
    IFeatureFlagRepository featureFlagRepository)
    : IQueryHandler<GetServiceDependantsQuery, IReadOnlyList<ServiceDependantDto>>
{
    public async ValueTask<Result<IReadOnlyList<ServiceDependantDto>>> Handle(
        GetServiceDependantsQuery query, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdWithServicesAsync(query.EnvironmentId, cancellationToken);
        if (environment is null || environment.ProjectId != query.ProjectId)
            return Error.NotFoundFor("Environment", query.EnvironmentId);

        if (environment.Services.All(s => s.Id != query.ServiceId))
            return Error.NotFoundFor("Service", query.ServiceId);

        var values = new Dictionary<Guid, IReadOnlyCollection<string?>>();
        foreach (var service in environment.Services)
        {
            var envVars = await environmentVariableRepository.GetForServiceAsync(service.Id, cancellationToken);
            var flags = await featureFlagRepository.GetForServiceListAsync(service.Id, cancellationToken);
            values[service.Id] = envVars.Select(v => v.Value)
                .Concat(flags.Select(f => (string?)f.Value))
                .ToList();
        }

        var graph = ServiceGraph.FromEnvironment(environment, values);
        var dependants = graph.GetDependants(query.ServiceId)
            .Select(n => new ServiceDependantDto(n.Service.Id, n.Service.Name, n.Service.Alias))
            .ToList();

        return Result<IReadOnlyList<ServiceDependantDto>>.Success(dependants);
    }
}
