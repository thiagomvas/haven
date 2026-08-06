using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Common.Models;

namespace Haven.Application.Features.Git.Queries.GetAccessibleRepositories;

public sealed class GetAccessibleRepositoriesHandler(
    IGitService gitService,
    IGitCredentialsRepository gitCredentialsRepository)
    : IQueryHandler<GetAccessibleRepositoriesQuery, IReadOnlyList<GitRepositorySummary>>
{
    public async ValueTask<Result<IReadOnlyList<GitRepositorySummary>>> Handle(GetAccessibleRepositoriesQuery query, CancellationToken cancellationToken)
    {
        var credentials = await gitCredentialsRepository.FindByIdAsync(query.GitCredentialId, cancellationToken);
        if (credentials is null)
            return Error.NotFoundFor(nameof(Domain.Entities.GitCredentials), query.GitCredentialId);

        return await gitService.GetAccessibleRepositoriesAsync(credentials, cancellationToken);
    }
}