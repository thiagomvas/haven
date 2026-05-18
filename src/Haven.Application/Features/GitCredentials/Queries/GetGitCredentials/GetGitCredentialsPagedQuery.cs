using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Queries.GetGitCredentials;

public class GetGitCredentialsPagedQuery : IPagedQuery<GitCredentialDto>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}