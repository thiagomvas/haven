using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services;
using Haven.Domain;
using Haven.Domain.Aggregates;

using Riok.Mapperly.Abstractions;

using Environment = Haven.Domain.Aggregates.Environment;
using Service = Haven.Domain.Aggregates.Service;


namespace Haven.Application.Mappers;

[Mapper]
public static partial class EnvironmentMapper
{
    [MapperIgnoreSource(nameof(Environment.Services))]
    [MapperIgnoreSource(nameof(Environment.ProjectId))]
    [MapperIgnoreSource(nameof(Environment.DomainEvents))]
    [MapperIgnoreSource(nameof(Environment.Project))]
    private static partial EnvironmentManifestDto ToManifestPartial(this Environment environment);
    
    public static EnvironmentManifestDto ToManifest(this Environment environment, bool includeServices = false)
    {
        var dto = environment.ToManifestPartial();
        if (includeServices)
        {
            dto.Services = environment.Services.Select(s => s.ToManifest()).ToList();
        }
        return dto;
    }

    public static Environment ToEntity(this EnvironmentManifestDto dto, Project project)
        => Environment.Reconstitute(dto.Id, project.Id, dto.Name, dto.Alias, dto.Description, dto.NetworkName);

    public static EnvironmentDashboardDto ToDashboardDto(this Environment environment, Project? project = null, ServiceStateSnapshot? state = null)
    {
        state ??= ServiceStateSnapshot.Empty;
        var statistics = ServiceStatisticsCalculator.Compute(environment.Services, state);
        var services = environment.Services
            .Select(s => s.ToDashboardDto(state))
            .ToList();

        var serviceStatusMap = environment.Services.ToDictionary(s => s.Name, s => state.GetDisplayStatus(s.Id));

        return new EnvironmentDashboardDto
        {
            Id = environment.Id,
            Name = environment.Name,
            Alias = environment.Alias,
            Description = environment.Description,
            ProjectId = environment.ProjectId,
            ProjectName = project?.Name ?? string.Empty,
            NetworkName = environment.NetworkName,
            ServiceStatistics = statistics,
            Status = ServiceStatisticsCalculator.GetEnvironmentStatus(statistics),
            Services = services,
            ServiceStatusMap = serviceStatusMap
        };
    }
}