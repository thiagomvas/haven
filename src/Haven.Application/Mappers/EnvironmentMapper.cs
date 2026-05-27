using Haven.Application.Features.Environments;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Models;
using Riok.Mapperly.Abstractions;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Application.Mappers;

[Mapper]
public static partial class EnvironmentMapper
{
    [MapperIgnoreSource(nameof(Environment.Services))]
    public static partial EnvironmentManifestDto ToManifest(this Environment environment);

    public static Environment ToEntity(this EnvironmentManifestDto dto, Project project)
        => Environment.Reconstitute(dto.Id, project.Id, dto.Name, dto.Description, dto.NetworkName);

    public static EnvironmentData ToEnvironmentData(this EnvironmentManifestDto dto, IEnumerable<ServiceData>? services = null)
        => new(dto.Id, dto.ProjectId, dto.Name, dto.Description, dto.NetworkName, services);

    public static EnvironmentDashboardDto ToDashboardDto(this Environment environment)
        => new()
        {
            Id = environment.Id,
            Name = environment.Name,
            NetworkName = environment.NetworkName,
            TotalServices = environment.Services.Count,
            ServicesRunning = environment.Services.Count(s => s.Status == ServiceStatus.Running),
            Status = environment.GetStatus()
        };
}
