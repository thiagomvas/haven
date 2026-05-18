using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.GitCredentials.Queries.GetGitCredentials;

public class GetGitCredentialsPagedHandler(IGitCredentialsRepository repository) : IPagedQueryHandler<GetGitCredentialsPagedQuery, GitCredentialDto>
{
    public async ValueTask<Result<PagedResult<GitCredentialDto>>> Handle(GetGitCredentialsPagedQuery query, CancellationToken cancellationToken)
    {
        var creds = await repository.GetPagedAsync(query.PageNumber, query.PageSize, cancellationToken);
        return Result<PagedResult<GitCredentialDto>>.Success(creds.Project(g => g.ToDto()));
    }
}