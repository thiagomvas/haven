using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Messaging;
using Haven.Application.Common.Models;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Enums;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.Backups.Queries.ListGitCommits;

public sealed class ListGitCommitsHandler(
    IGitProviderFactory gitProviderFactory,
    IOptionsMonitor<ManifestsOptions> manifestsOptions)
    : IQueryHandler<ListGitCommitsQuery, IReadOnlyList<GitCommitInfo>>
{
    public async ValueTask<Result<IReadOnlyList<GitCommitInfo>>> Handle(ListGitCommitsQuery request, CancellationToken ct)
    {
        var manifestsPath = manifestsOptions.CurrentValue.ManifestsPath;
        if (!Directory.Exists(manifestsPath))
            return Result<IReadOnlyList<GitCommitInfo>>.Success([]);

        var gitProvider = gitProviderFactory.Create(GitProviderType.Generic);
        var commits = await gitProvider.GetCommitsAsync(manifestsPath, request.Limit, ct);
        return Result<IReadOnlyList<GitCommitInfo>>.Success(commits);
    }
}