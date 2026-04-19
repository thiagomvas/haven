using Haven.Application.Features.Services;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class ServiceMapper
{
    public static partial ServiceManifestDto ToManifest(this Service service);

    public static Project.ServiceData ToServiceData(this ServiceManifestDto dto)
        => new(dto.Id, dto.EnvironmentId, dto.Name, dto.Type, dto.ExposureMode, dto.Status, dto.CreatedAt, dto.UpdatedAt);
}
