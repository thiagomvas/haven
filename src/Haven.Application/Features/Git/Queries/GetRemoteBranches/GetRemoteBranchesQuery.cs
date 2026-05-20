using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Git.Queries.GetRemoteBranches;

public sealed class GetRemoteBranchesQuery : IQuery<IReadOnlyList<string>>
{
    public string RepositoryUrl { get; set; } = string.Empty;
    public Guid? GitCredentialId { get; set; }
}
