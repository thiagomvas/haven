using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Deployments.Queries.GetRecentDeployments;

public sealed class GetRecentDeploymentsHandler(IDeploymentRepository deploymentRepository)
    : IQueryHandler<GetRecentDeploymentsQuery, List<RecentDeploymentDto>>
{
    private const int MaxLimit = 50;

    public async ValueTask<Result<List<RecentDeploymentDto>>> Handle(GetRecentDeploymentsQuery query, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, MaxLimit);
        var deployments = await deploymentRepository.GetRecentAsync(limit, cancellationToken);

        var dtos = deployments
            .Where(d => d.Service?.Environment?.Project is not null)
            .Select(d => new RecentDeploymentDto
            {
                Id = d.Id,
                ServiceId = d.ServiceId,
                ServiceName = d.Service!.Name,
                ProjectId = d.Service.Environment!.ProjectId,
                ProjectName = d.Service.Environment.Project!.Name,
                EnvironmentId = d.Service.EnvironmentId,
                EnvironmentName = d.Service.Environment.Name,
                Status = d.Status,
                TriggeredBy = d.TriggeredBy,
                IdentifierType = d.IdentifierType,
                Identifier = d.Identifier,
                CommitMessage = d.CommitMessage,
                StartedAt = d.StartedAt,
                FinishedAt = d.FinishedAt,
            })
            .ToList();

        return Result<List<RecentDeploymentDto>>.Success(dtos);
    }
}