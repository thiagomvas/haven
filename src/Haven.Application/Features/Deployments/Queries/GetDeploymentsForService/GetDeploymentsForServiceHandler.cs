using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Deployments.Queries.GetDeploymentsForService;

public sealed class GetDeploymentsForServiceHandler(
    IServiceRepository serviceRepository,
    IDeploymentRepository deploymentRepository)
    : IQueryHandler<GetDeploymentsForServiceQuery, List<DeploymentDto>>
{
    public async ValueTask<Result<List<DeploymentDto>>> Handle(GetDeploymentsForServiceQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null || service.EnvironmentId != query.EnvironmentId)
            return Error.NotFoundFor("Service", query.ServiceId);

        var deployments = await deploymentRepository.GetAllForServiceAsync(query.ServiceId, cancellationToken);

        var dtos = deployments.Select(d => new DeploymentDto
        {
            Id = d.Id,
            ServiceId = d.ServiceId,
            StartedAt = d.StartedAt,
            FinishedAt = d.FinishedAt,
            Status = d.Status,
            TriggeredBy = d.TriggeredBy,
        }).ToList();

        return Result<List<DeploymentDto>>.Success(dtos);
    }
}