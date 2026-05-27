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
        var projectEnvVarKeys = projectEnvVars.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);

        var dto = project.ToDashboardDto(projectEnvVars);

        foreach (var environment in dto.Environments)
        {
            var envVars = await environmentVariableRepository.GetForEnvironmentAsync(environment.Id, cancellationToken);
            environment.TotalEnvVars = envVars.Count();

            environment.EnvVarOverrides = envVars
                .Where(x => projectEnvVarKeys.ContainsKey(x.Key))
                .ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
        }

        return Result<ProjectDashboardDto>.Success(dto);
    }
}
