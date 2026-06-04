using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Environments.Queries.GetEnvironmentDashboard;

public sealed class GetEnvironmentDashboardHandler(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    IEnvironmentVariableService environmentVariableService)
    : IQueryHandler<GetEnvironmentDashboardQuery, EnvironmentDashboardDto>
{
    public async ValueTask<Result<EnvironmentDashboardDto>> Handle(GetEnvironmentDashboardQuery query, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var environment = await environmentRepository.GetByIdAsync(query.EnvironmentId, cancellationToken);
        if (environment is null || environment.ProjectId != query.ProjectId)
            return Error.NotFoundFor("Environment", query.EnvironmentId);

        var envVars = await environmentVariableService.BuildVariablesForEnvironmentAsync(query.EnvironmentId, cancellationToken);

        var dto = environment.ToDashboardDto(project);
        dto.TotalEnvVars = envVars.Count();
        dto.EnvironmentVariables = envVars.Select(x => x.ToDto()).ToList();

        return Result<EnvironmentDashboardDto>.Success(dto);
    }
}
