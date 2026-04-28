using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjects;

namespace Haven.Application.Features.Projects.Queries.GetProject;

public sealed class GetProjectQuery : IQuery<ProjectDto>
{
    public Guid ProjectId { get; init; }
}
