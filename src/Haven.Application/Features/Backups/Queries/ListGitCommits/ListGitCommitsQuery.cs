using Haven.Application.Common;
using Haven.Application.Common.Models;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Backups.Queries.ListGitCommits;

[RequirePermission(Permissions.System.ManageBackups)]
public sealed record ListGitCommitsQuery(int Limit = 50) : IQuery<IReadOnlyList<GitCommitInfo>>;
