using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Projects.Queries.GetProjectDashboard;

public sealed class GetProjectDashboardHandler(IProjectRepository repository)
    : IQueryHandler<GetProjectDashboardQuery, ProjectDashboardDto>
{
    public async ValueTask<Result<ProjectDashboardDto>> Handle(GetProjectDashboardQuery query, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var dto = project.ToDashboardDto();
        return Result<ProjectDashboardDto>.Success(dto);
    }
}
