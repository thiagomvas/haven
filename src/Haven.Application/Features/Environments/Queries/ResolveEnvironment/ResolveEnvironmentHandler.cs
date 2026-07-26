using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Queries.ResolveEnvironment;

public sealed class ResolveEnvironmentHandler(IEnvironmentRepository environmentRepository)
    : IQueryHandler<ResolveEnvironmentQuery, EnvironmentLocationDto>
{
    public async ValueTask<Result<EnvironmentLocationDto>> Handle(ResolveEnvironmentQuery query, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(query.EnvironmentId, cancellationToken);
        if (environment is null)
            return Error.NotFoundFor("Environment", query.EnvironmentId);

        return Result<EnvironmentLocationDto>.Success(new EnvironmentLocationDto
        {
            EnvironmentId = environment.Id,
            ProjectId = environment.ProjectId,
        });
    }
}
