using System.Text.Json;

using Haven.Application.Common.Interfaces.Deployment;

using Microsoft.Extensions.Options;

using Octokit;

namespace Haven.Infrastructure.Deployment.Git;

public class GitHubOAuthService(IHttpClientFactory httpClientFactory, IOptions<GitHubAppOptions> options)
    : IGitHubOAuthService
{
    private const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
    private const string TokenUrl = "https://github.com/login/oauth/access_token";

    public string BuildAuthorizeUrl(string state)
    {
        var opts = options.Value;
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = opts.ClientId,
            ["redirect_uri"] = opts.RedirectUri,
            ["state"] = state
        };

        var queryString = string.Join("&",
            query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? string.Empty)}"));

        return $"{AuthorizeUrl}?{queryString}";
    }

    public Task<GitHubOAuthTokenResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var form = new Dictionary<string, string>
        {
            ["client_id"] = opts.ClientId,
            ["client_secret"] = opts.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = opts.RedirectUri
        };

        return RequestTokenAsync(form, cancellationToken);
    }

    public Task<GitHubOAuthTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var form = new Dictionary<string, string>
        {
            ["client_id"] = opts.ClientId,
            ["client_secret"] = opts.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        return RequestTokenAsync(form, cancellationToken);
    }

    private async Task<GitHubOAuthTokenResult> RequestTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("github-oauth");

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (root.TryGetProperty("error", out var errorElement))
        {
            var description = root.TryGetProperty("error_description", out var descElement)
                ? descElement.GetString()
                : null;
            throw new InvalidOperationException(
                $"GitHub OAuth token request failed: {errorElement.GetString()} - {description}");
        }

        var accessToken = root.GetProperty("access_token").GetString()
                           ?? throw new InvalidOperationException("GitHub OAuth response did not contain an access token.");
        var refreshToken = root.TryGetProperty("refresh_token", out var refreshTokenElement)
            ? refreshTokenElement.GetString()
            : null;

        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? accessTokenExpiresAt = root.TryGetProperty("expires_in", out var expiresInElement) &&
                                                expiresInElement.TryGetInt64(out var expiresIn)
            ? now.AddSeconds(expiresIn)
            : null;

        return new GitHubOAuthTokenResult(accessToken, refreshToken, accessTokenExpiresAt);
    }

    public async Task<string> GetAuthenticatedUserLoginAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var client = new GitHubClient(new ProductHeaderValue("Haven"))
        {
            Credentials = new Credentials(accessToken)
        };

        var user = await client.User.Current();
        return user.Login;
    }
}