using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjects;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Projects.Queries.GetProject;

public sealed class GetProjectHandler(IProjectRepository repository)
    : IQueryHandler<GetProjectQuery, ProjectDto>
{
    public async ValueTask<Result<ProjectDto>> Handle(GetProjectQuery query, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var dto = new ProjectDto(
            project.Id,
            project.Name,
            project.Alias,
            project.Description,
            project.Environments.Count,
            project.Environments.Sum(e => e.Services.Count));
        return Result<ProjectDto>.Success(dto);
    }
}