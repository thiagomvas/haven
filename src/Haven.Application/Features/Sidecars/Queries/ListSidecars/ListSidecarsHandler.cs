using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Sidecars.Queries.ListSidecars;

public sealed class ListSidecarsHandler(ISidecarRepository sidecarRepository)
    : IQueryHandler<ListSidecarsQuery, IReadOnlyList<SidecarDto>>
{
    public async ValueTask<Result<IReadOnlyList<SidecarDto>>> Handle(ListSidecarsQuery query, CancellationToken cancellationToken)
    {
        var sidecars = await sidecarRepository.GetAllAsync(cancellationToken);

        var items = sidecars
            .Select(s =>
            {
                var dockerConfig = s.SourceConfig as DockerConfig;
                return new SidecarDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Alias = s.Alias,
                    Kind = s.Kind,
                    Status = s.Status,
                    Health = s.Health,
                    Enabled = s.Enabled,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    LastDeployedAt = s.LastDeployedAt,
                    Image = dockerConfig?.Image,
                    Ports = dockerConfig?.Ports ?? [],
                    CommandArgs = dockerConfig?.CommandArgs ?? [],
                    RestartPolicy = dockerConfig?.RestartPolicy,
                    IsAcmeConfigured = s.Kind == Domain.Enums.SidecarKind.Traefik
                        ? dockerConfig?.HasAcmeResolverConfigured() ?? false
                        : null
                };
            })
            .ToList();

        return Result<IReadOnlyList<SidecarDto>>.Success(items);
    }
}