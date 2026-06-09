using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Environments.Queries.GetEnvironment;

public sealed class GetEnvironmentHandler(IProjectRepository projectRepository, IEnvironmentRepository environmentRepository)
    : IQueryHandler<GetEnvironmentQuery, EnvironmentDto>
{
    public async ValueTask<Result<EnvironmentDto>> Handle(GetEnvironmentQuery query, CancellationToken cancellationToken)
    {
        var projectExists = await projectRepository.GetByIdAsync(query.ProjectId, cancellationToken) is not null;
        if (!projectExists)
            return Error.NotFoundFor(nameof(Project), query.ProjectId);

        var environment = await environmentRepository.GetByIdAsync(query.EnvironmentId, cancellationToken);
        if (environment is null || environment.ProjectId != query.ProjectId)
            return Error.NotFoundFor("Environment", query.EnvironmentId);

        var dto = new EnvironmentDto(
            environment.Id,
            environment.ProjectId,
            environment.Name,
            environment.Alias,
            environment.Description,
            environment.NetworkName,
            environment.Services.Count);
        return Result<EnvironmentDto>.Success(dto);
    }
}