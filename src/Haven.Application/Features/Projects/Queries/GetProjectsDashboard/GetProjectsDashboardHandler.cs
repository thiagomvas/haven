using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class GetProjectsDashboardHandler(IProjectRepository repository)
    : IPagedQueryHandler<GetProjectsDashboardQuery, ProjectDashboardDto>
{
    public async ValueTask<PagedResult<ProjectDashboardDto>> Handle(GetProjectsDashboardQuery query, CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);

        return paged.Project(project =>
        {
            var environments = project.Environments
                .Select(env => new EnvironmentDashboardDto
                {
                    Id = env.Id,
                    Name = env.Name,
                    TotalServices = env.Services.Count,
                    ServicesRunning = env.Services.Count(s => s.Status == ServiceStatus.Running)
                })
                .ToList();

            var allServices = project.Environments.SelectMany(e => e.Services).ToList();
            var totalServices = allServices.Count;
            var totalServicesRunning = allServices.Count(s => s.Status == ServiceStatus.Running);
            var lastDeployed = allServices
                .Where(s => s.LastDeployedAt.HasValue)
                .Select(s => s.LastDeployedAt!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            return new ProjectDashboardDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Environments = environments,
                TotalServices = totalServices,
                TotalServicesRunning = totalServicesRunning,
                LastDeployedAt = lastDeployed == DateTime.MinValue ? null : lastDeployed
            };
        });
    }
}
