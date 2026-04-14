using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsHandler(IProjectRepository repository)
    : IPagedQueryHandler<GetProjectsQuery, ProjectDto>
{
    public async ValueTask<Result<PagedResult<ProjectDto>>> Handle(GetProjectsQuery query, CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);

        var items = paged.Items
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description))
            .ToList();

        return Result<PagedResult<ProjectDto>>.Success(
            new PagedResult<ProjectDto>(items, paged.TotalCount, paged.PageNumber, paged.PageSize));
    }
}
