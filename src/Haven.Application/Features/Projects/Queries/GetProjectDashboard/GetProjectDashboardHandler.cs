using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Mappers;
using Haven.Domain;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Projects.Queries.GetProjectDashboard;

public sealed class GetProjectDashboardHandler(
    IProjectRepository repository,
    IEnvironmentVariableRepository environmentVariableRepository)
    : IQueryHandler<GetProjectDashboardQuery, ProjectDashboardDto>
{
    public async ValueTask<Result<ProjectDashboardDto>> Handle(GetProjectDashboardQuery query, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var projectEnvVars = await environmentVariableRepository.GetForProjectAsync(project.Id, cancellationToken);

        var dto = project.ToDashboardDto(projectEnvVars);

        foreach (var environment in dto.Environments)
        {
            var envVars = await environmentVariableRepository.GetForEnvironmentAsync(environment.Id, cancellationToken);
            var envVarsArr = envVars as Domain.Entities.EnvironmentVariables[] ?? envVars.ToArray();
            environment.TotalEnvVars = envVarsArr.Length;
            environment.EnvironmentVariables = envVarsArr.Select(x => x.ToDto()).ToList();
        }

        return Result<ProjectDashboardDto>.Success(dto);
    }
}
