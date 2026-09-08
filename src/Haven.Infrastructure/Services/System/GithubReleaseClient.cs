using System.Net.Http.Json;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Services.System;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services.System;

public sealed class GithubReleaseClient(
    IHttpClientFactory httpClientFactory,
    ILogger<GithubReleaseClient> logger) : IGithubReleaseClient
{
    private const string Owner = "thiagomvas";
    private const string Repo = "haven";

    public async Task<Result<IReadOnlyList<GithubRelease>>> GetReleasesAsync(CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(nameof(GithubReleaseClient));
            using var response = await client.GetAsync($"repos/{Owner}/{Repo}/releases", ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to fetch GitHub releases for {Owner}/{Repo}: {StatusCode}",
                    Owner, Repo, response.StatusCode);
                return Error.Failed;
            }

            var releases = await response.Content.ReadFromJsonAsync<List<GithubRelease>>(ct);
            return Result<IReadOnlyList<GithubRelease>>.Success(releases ?? []);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogDebug(ex, "Failed to reach GitHub API to fetch releases for {Owner}/{Repo}", Owner, Repo);
            return Error.Failed;
        }
    }
}