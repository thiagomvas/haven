using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Sidecars.Queries.ListSidecars;

public sealed class ListSidecarsHandler(ISidecarRepository sidecarRepository)
    : IQueryHandler<ListSidecarsQuery, IReadOnlyList<SidecarDto>>
{
    public async ValueTask<Result<IReadOnlyList<SidecarDto>>> Handle(ListSidecarsQuery query, CancellationToken cancellationToken)
    {
        var sidecars = await sidecarRepository.GetAllAsync(cancellationToken);

        var items = sidecars
            .Select(s => new SidecarDto
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
                LastDeployedAt = s.LastDeployedAt
            })
            .ToList();

        return Result<IReadOnlyList<SidecarDto>>.Success(items);
    }
}