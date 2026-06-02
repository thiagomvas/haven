using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Git.Queries.GetRemoteBranches;

[RequirePermission(Permissions.Credentials.View)]
public sealed class GetRemoteBranchesQuery : IQuery<IReadOnlyList<string>>
{
    public string RepositoryUrl { get; set; } = string.Empty;
    public Guid? GitCredentialId { get; set; }
}
