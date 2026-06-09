using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class GetProjectsDashboardHandler(IProjectRepository repository)
    : IPagedQueryHandler<GetProjectsDashboardQuery, ProjectDashboardDto>
{
    public async ValueTask<PagedResult<ProjectDashboardDto>> Handle(GetProjectsDashboardQuery query, CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);

        return paged.Project(project => project.ToDashboardDto());
    }
}