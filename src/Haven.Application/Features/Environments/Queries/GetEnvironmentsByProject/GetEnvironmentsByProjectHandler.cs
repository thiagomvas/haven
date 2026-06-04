using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Environments.Queries;
using Haven.Domain.Aggregates;


namespace Haven.Application.Features.Environments.Queries.GetEnvironmentsByProject;

public sealed class GetEnvironmentsByProjectHandler(IProjectRepository projectRepository, IEnvironmentRepository environmentRepository)
    : IQueryHandler<GetEnvironmentsByProjectQuery, IReadOnlyList<EnvironmentDto>>
{
    public async ValueTask<Result<IReadOnlyList<EnvironmentDto>>> Handle(GetEnvironmentsByProjectQuery query, CancellationToken cancellationToken)
    {
        var projectExists = await projectRepository.GetByIdAsync(query.ProjectId, cancellationToken) is not null;
        if (!projectExists)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var environments = await environmentRepository.GetByProjectIdAsync(query.ProjectId, cancellationToken);

        var items = environments
            .Select(e => new EnvironmentDto(
                e.Id,
                e.ProjectId,
                e.Name,
                e.Alias,
                e.Description,
                e.NetworkName,
                e.Services.Count))
            .ToList();

        return Result<IReadOnlyList<EnvironmentDto>>.Success(items);
    }
}
