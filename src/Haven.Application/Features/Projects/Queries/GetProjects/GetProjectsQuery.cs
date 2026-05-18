using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQuery : PagedQuery<ProjectDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
