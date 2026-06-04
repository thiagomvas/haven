using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Models;
using Riok.Mapperly.Abstractions;
using Service = Haven.Domain.Entities.Service;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Application.Mappers;

[Mapper]
public static partial class EnvironmentMapper
{
    [MapperIgnoreSource(nameof(Environment.Services))]
    public static partial EnvironmentManifestDto ToManifest(this Environment environment);

    public static Environment ToEntity(this EnvironmentManifestDto dto, Project project)
        => Environment.Reconstitute(dto.Id, project.Id, dto.Name, dto.Alias, dto.Description, dto.NetworkName);

    public static EnvironmentData ToEnvironmentData(this EnvironmentManifestDto dto, IEnumerable<ServiceData>? services = null)
        => new(dto.Id, dto.ProjectId, dto.Name, dto.Alias, dto.Description, dto.NetworkName, services);

    public static EnvironmentDashboardDto ToDashboardDto(this Environment environment, Project? project = null)
    {
        var (total, running, stopped, degraded, deploymentPending, deploying, unknown) = environment.GetServiceStatistics();
        var services = environment.Services
            .Select(s => s.ToDashboardDto())
            .ToList();

        var serviceStatusMap = environment.Services.ToDictionary(s => s.Name, s => s.Status);

        return new EnvironmentDashboardDto
        {
            Id = environment.Id,
            Name = environment.Name,
            Alias = environment.Alias,
            Description = environment.Description,
            ProjectId = environment.ProjectId,
            ProjectName = project?.Name ?? string.Empty,
            NetworkName = environment.NetworkName,
            ServiceStatistics = new ServiceStatisticsDto
            {
                Total = total,
                Running = running,
                Stopped = stopped,
                Degraded = degraded,
                DeploymentPending = deploymentPending,
                Deploying = deploying,
                Unknown = unknown
            },
            Status = environment.GetStatus(),
            Services = services,
            ServiceStatusMap = serviceStatusMap
        };
    }
}
