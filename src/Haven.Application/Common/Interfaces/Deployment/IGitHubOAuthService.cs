namespace Haven.Application.Common.Interfaces.Deployment;

public interface IGitHubOAuthService
{
    /// <summary>
    /// Builds the GitHub App authorize URL for the user-to-server OAuth flow.
    /// </summary>
    string BuildAuthorizeUrl(string state);

    /// <summary>
    /// Exchanges an authorization code for a user access token.
    /// </summary>
    Task<GitHubOAuthTokenResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a refresh token for a new access token.
    /// </summary>
    Task<GitHubOAuthTokenResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public record GitHubOAuthTokenResult(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? AccessTokenExpiresAt);
