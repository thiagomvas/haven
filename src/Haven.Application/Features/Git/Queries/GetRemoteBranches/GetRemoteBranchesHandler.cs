using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Git.Queries.GetRemoteBranches;

public sealed class GetRemoteBranchesHandler(
    IGitService gitService,
    IGitCredentialsRepository gitCredentialsRepository)
    : IQueryHandler<GetRemoteBranchesQuery, IReadOnlyList<string>>
{
    public async ValueTask<Result<IReadOnlyList<string>>> Handle(GetRemoteBranchesQuery query, CancellationToken cancellationToken)
    {
        Domain.Entities.GitCredentials? credentials = null;

        if (query.GitCredentialId.HasValue)
        {
            credentials = await gitCredentialsRepository.FindByIdAsync(query.GitCredentialId.Value, cancellationToken);
            if (credentials is null)
                return Error.NotFoundFor(nameof(Domain.Entities.GitCredentials), query.GitCredentialId.Value);
        }

        return await gitService.GetRemoteBranchesAsync(query.RepositoryUrl, credentials, cancellationToken);
    }
}