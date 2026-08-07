using System.Text.RegularExpressions;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

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
    private static readonly TimeSpan ValidationThrottle = TimeSpan.FromHours(1);

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
        await MarkValidatedIfStaleAsync(cancellationToken);
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

        const int pageSize = 100;
        var repos = new List<Repository>();
        var page = 1;
        while (true)
        {
            var options = new ApiOptions { PageSize = pageSize, PageCount = 1, StartPage = page };
            var batch = await client.Repository.GetAllForCurrent(request, options);
            repos.AddRange(batch);

            if (batch.Count < pageSize)
                break;

            page++;
        }

        var summaries = repos.Select(r => new GitRepositorySummary(r.Name, r.FullName, r.CloneUrl, r.Private)).ToList();

        cache.Set(cacheKey, (IReadOnlyList<GitRepositorySummary>)summaries, RepositoryCacheTtl);

        await MarkValidatedIfStaleAsync(cancellationToken);

        return summaries;
    }

    private static string RepositoryCacheKey(Guid credentialsId) => $"github-repos:{credentialsId}";

    /// <summary>
    /// Bumps LastValidatedAt after a successful API call, throttled so routine
    /// autocomplete/cache-refresh traffic doesn't cause a write on every request.
    /// </summary>
    internal async Task MarkValidatedIfStaleAsync(CancellationToken cancellationToken)
    {
        if (credentials is null)
            return;

        if (DateTimeOffset.UtcNow - credentials.LastValidatedAt < ValidationThrottle)
            return;

        credentials.MarkValidated();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> EnsureValidTokenAsync(CancellationToken cancellationToken)
    {
        if (credentials is null)
            throw new InvalidOperationException("GitHub credentials are required to query the GitHub API.");

        await EnsureCredentialsFreshAsync(cancellationToken);

        return credentials.PrimaryCredential.Value;
    }

    /// <summary>
    /// Refreshes an about-to-expire OAuth access token in place before the credential is read by an API
    /// call or by the clone/pull/push credential providers inherited from <see cref="GenericGitProvider"/>.
    /// </summary>
    protected override async Task EnsureCredentialsFreshAsync(CancellationToken cancellationToken)
    {
        if (credentials is null)
            return;

        var needsRefresh = credentials.AuthMethod == GitAuthMethod.OAuth &&
                            credentials.SecondaryCredential is not null &&
                            credentials.AccessTokenExpiresAt is { } expiresAt &&
                            expiresAt <= DateTimeOffset.UtcNow.AddSeconds(60);

        if (!needsRefresh)
            return;

        var result = await oauthService.RefreshTokenAsync(credentials.SecondaryCredential!.Value, cancellationToken);
        credentials.UpdateOAuthTokens(result.AccessToken, result.RefreshToken, result.AccessTokenExpiresAt);
        await unitOfWork.SaveChangesAsync(cancellationToken);
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