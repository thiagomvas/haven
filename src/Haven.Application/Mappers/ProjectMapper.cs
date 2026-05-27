using Haven.Application.Features.Projects;
using Haven.Application.Features.Projects.Queries.GetProjects;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Models;
using Riok.Mapperly.Abstractions;


namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ProjectMapper
{
    [MapperIgnoreSource(nameof(Project.DomainEvents))]
    [MapperIgnoreSource(nameof(Project.Environments))]
    public static partial ProjectManifestDto ToManifest(this Project project);

    public static Project FromManifest(this ProjectManifestDto dto, IEnumerable<EnvironmentData>? environments = null)
        => Project.Reconstitute(dto.Id, dto.Name, dto.Description, environments);

    private static partial ProjectDto ToDtoPartial(this Project project);

    public static ProjectDto ToDto(this Project project)
    {
        return project.ToDtoPartial();
    }

    public static ProjectDashboardDto ToDashboardDto(this Project project, IEnumerable<EnvironmentVariables>? projectEnvVars = null)
    {
        var environments = project.Environments
            .Select(env => env.ToDashboardDto())
            .ToList();

        var allServices = project.Environments.SelectMany(e => e.Services).ToList();
        var totalServices = allServices.Count;
        var totalServicesRunning = allServices.Count(s => s.Status == ServiceStatus.Running);
        var lastDeployed = allServices
            .Where(s => s.LastDeployedAt.HasValue)
            .Select(s => s.LastDeployedAt!.Value)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        return new ProjectDashboardDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Environments = environments,
            TotalServices = totalServices,
            TotalServicesRunning = totalServicesRunning,
            LastDeployedAt = lastDeployed == DateTime.MinValue ? null : lastDeployed,
            TotalEnvVars = projectEnvVars?.Count() ?? 0
        };
    }
}