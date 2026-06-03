using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Queries.GetGitCredentials;

[RequirePermission(Permissions.System.ManageGitCredentials)]
public class GetGitCredentialsPagedQuery : PagedQuery<GitCredentialDto>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}