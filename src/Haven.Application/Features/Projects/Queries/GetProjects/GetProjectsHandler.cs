using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsHandler(IProjectRepository repository)
    : IPagedQueryHandler<GetProjectsQuery, ProjectDto>
{
    public async ValueTask<PagedResult<ProjectDto>> Handle(GetProjectsQuery query, CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);
        return paged.Project(p => p.ToDto());
    }
}
