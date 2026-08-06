using System.Text.RegularExpressions;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

namespace Haven.Infrastructure.Deployment.Git;

public partial class GitHubGitProvider(
    GitCredentials? credentials,
    ILogger<GitHubGitProvider> logger,
    IGitHubOAuthService oauthService,
    IUnitOfWork unitOfWork,
    IMemoryCache cache) : GenericGitProvider(credentials, logger)
{
    private static readonly TimeSpan RepositoryCacheTtl = TimeSpan.FromMinutes(2);

    public override GitProviderType Type => GitProviderType.GitHub;

    public override async Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        var (owner, repo) = ParseOwnerAndRepo(repositoryUrl);
        var accessToken = await EnsureValidTokenAsync(cancellationToken);

        var client = new GitHubClient(new ProductHeaderValue("Haven"))
        {
            Credentials = new Credentials(accessToken)
        };

        var branches = await client.Repository.Branch.GetAll(owner, repo);
        return branches.Select(b => b.Name).ToList();
    }

    public override async Task<IReadOnlyList<GitRepositorySummary>> GetAccessibleRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        if (credentials is null)
            throw new InvalidOperationException("GitHub credentials are required to query the GitHub API.");

        var cacheKey = RepositoryCacheKey(credentials.Id);
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<GitRepositorySummary>? cached) && cached is not null)
            return cached;

        var accessToken = await EnsureValidTokenAsync(cancellationToken);

        var client = new GitHubClient(new ProductHeaderValue("Haven"))
        {
            Credentials = new Credentials(accessToken)
        };

        var request = new RepositoryRequest
        {
            Affiliation = RepositoryAffiliation.All,
            Sort = RepositorySort.Updated,
            Direction = SortDirection.Descending,
        };
        var options = new ApiOptions { PageSize = 100 };

        var repos = await client.Repository.GetAllForCurrent(request, options);
        var summaries = repos.Select(r => new GitRepositorySummary(r.Name, r.FullName, r.CloneUrl, r.Private)).ToList();

        cache.Set(cacheKey, (IReadOnlyList<GitRepositorySummary>)summaries, RepositoryCacheTtl);

        return summaries;
    }

    private static string RepositoryCacheKey(Guid credentialsId) => $"github-repos:{credentialsId}";

    private async Task<string> EnsureValidTokenAsync(CancellationToken cancellationToken)
    {
        if (credentials is null)
            throw new InvalidOperationException("GitHub credentials are required to query the GitHub API.");

        var needsRefresh = credentials.AuthMethod == GitAuthMethod.OAuth &&
                            credentials.SecondaryCredential is not null &&
                            credentials.AccessTokenExpiresAt is { } expiresAt &&
                            expiresAt <= DateTimeOffset.UtcNow.AddSeconds(60);

        if (needsRefresh)
        {
            var result = await oauthService.RefreshTokenAsync(credentials.SecondaryCredential!.Value, cancellationToken);
            credentials.UpdateOAuthTokens(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAt);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return credentials.PrimaryCredential.Value;
    }

    private static (string Owner, string Repo) ParseOwnerAndRepo(string repositoryUrl)
    {
        var match = GitHubRepositoryUrlRegex().Match(repositoryUrl);
        if (!match.Success)
            throw new ArgumentException($"Unable to parse GitHub owner/repo from URL: {repositoryUrl}", nameof(repositoryUrl));

        return (match.Groups["owner"].Value, match.Groups["repo"].Value);
    }

    [GeneratedRegex(@"github\.com[:/](?<owner>[^/]+)/(?<repo>[^/]+?)(\.git)?/?$")]
    private static partial Regex GitHubRepositoryUrlRegex();
}