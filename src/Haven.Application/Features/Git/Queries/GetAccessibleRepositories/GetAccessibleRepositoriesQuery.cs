using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Common.Models;

namespace Haven.Application.Features.Git.Queries.GetAccessibleRepositories;

[RequirePermission(Permissions.System.ReadGitCredentials)]
public sealed class GetAccessibleRepositoriesQuery : IQuery<IReadOnlyList<GitRepositorySummary>>
{
    public Guid GitCredentialId { get; set; }
}
