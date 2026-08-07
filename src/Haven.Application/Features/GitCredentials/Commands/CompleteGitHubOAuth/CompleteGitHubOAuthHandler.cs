using Haven.Application.Common;
using Haven.Application.Common.Exceptions;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Enums;

using Microsoft.Extensions.Options;

namespace Haven.Application.Features.GitCredentials.Commands.CompleteGitHubOAuth;

using GitCredentialsEntity = Haven.Domain.Entities.GitCredentials;

public sealed class CompleteGitHubOAuthHandler(
    IGitHubOAuthService oauthService,
    IGitCredentialsRepository credentialsRepository,
    IOptionsMonitor<GitHubAppOptions> githubAppOptions,
    IOptionsMonitor<NetworkOptions> networkOptions)
    : Common.Messaging.ICommandHandler<CompleteGitHubOAuthCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CompleteGitHubOAuthCommand request, CancellationToken cancellationToken)
    {
        var opts = githubAppOptions.CurrentValue;
        if (string.IsNullOrEmpty(opts.ClientId) || string.IsNullOrEmpty(opts.ClientSecret) ||
            networkOptions.CurrentValue.BuildHost() is null)
            throw new GitHubOAuthNotConfiguredException();

        var token = await oauthService.ExchangeCodeAsync(request.Code, cancellationToken);

        if (request.CredentialId.HasValue)
        {
            var existing = await credentialsRepository.GetByIdAsync(request.CredentialId.Value, cancellationToken);
            if (existing is null)
                return Error.NotFoundFor(nameof(GitCredentialsEntity), request.CredentialId.Value);

            existing.UpdateOAuthTokens(token.AccessToken, token.RefreshToken, token.AccessTokenExpiresAt);
            return Result<Guid>.Success(existing.Id);
        }

        var login = await oauthService.GetAuthenticatedUserLoginAsync(token.AccessToken, cancellationToken);

        var credentials = GitCredentialsEntity.CreateFromOAuth(
            GitProviderType.GitHub,
            hostUrl: null,
            token.AccessToken,
            token.RefreshToken,
            token.AccessTokenExpiresAt,
            login);

        var credentialsId = await credentialsRepository.AddAsync(credentials, cancellationToken);

        return Result<Guid>.CreatedFor(credentialsId);
    }
}