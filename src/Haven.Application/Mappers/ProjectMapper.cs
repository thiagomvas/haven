using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Projects;
using Haven.Application.Features.Projects.Queries.GetProjects;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using Riok.Mapperly.Abstractions;


namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ProjectMapper
{
    [MapperIgnoreSource(nameof(Project.DomainEvents))]
    [MapperIgnoreSource(nameof(Project.Environments))]
    private static partial ProjectManifestDto ToManifestPartial(this Project project);
    
    public static ProjectManifestDto ToManifest(this Project project, bool includeEnvironments = false, bool includeServices = false)
    {
        var dto = project.ToManifestPartial();
        if (includeEnvironments)
        {
            dto.Environments = project.Environments.Select(e => e.ToManifest(includeServices)).ToList();
        }
        return dto;
    }

    public static Project FromManifest(this ProjectManifestDto dto)
        => Project.Reconstitute(dto.Id, dto.Name, dto.Alias, dto.Description);

    private static partial ProjectDto ToDtoPartial(this Project project);

    public static ProjectDto ToDto(this Project project)
    {
        return project.ToDtoPartial();
    }

    public static ProjectDashboardDto ToDashboardDto(this Project project, IEnumerable<EnvironmentVariables>? projectEnvVars = null, ServiceStateSnapshot? state = null)
    {
        state ??= ServiceStateSnapshot.Empty;
        var environments = project.Environments
            .Select(env => env.ToDashboardDto(project, state))
            .ToList();

        var statistics = ServiceStatisticsCalculator.Sum(environments.Select(e => e.ServiceStatistics));
        var allServices = project.Environments.SelectMany(e => e.Services).ToList();
        var lastDeployed = allServices
            .Where(s => s.LastDeployedAt.HasValue)
            .Select(s => s.LastDeployedAt!.Value)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        var envs = projectEnvVars?.Select(e => e.ToDto()).ToList() ?? [];

        return new ProjectDashboardDto
        {
            Id = project.Id,
            Name = project.Name,
            Alias = project.Alias,
            Description = project.Description,
            Environments = environments,
            ServiceStatistics = statistics,
            LastDeployedAt = lastDeployed == DateTime.MinValue ? null : lastDeployed,
            TotalEnvVars = projectEnvVars?.Count() ?? 0,
            EnvironmentVariables = envs
        };
    }
}