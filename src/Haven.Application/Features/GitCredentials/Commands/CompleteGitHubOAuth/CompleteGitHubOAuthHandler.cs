using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;

namespace Haven.Application.Features.GitCredentials.Commands.CompleteGitHubOAuth;

using GitCredentialsEntity = Haven.Domain.Entities.GitCredentials;

public sealed class CompleteGitHubOAuthHandler(
    IGitHubOAuthService oauthService,
    IGitCredentialsRepository credentialsRepository)
    : Common.Messaging.ICommandHandler<CompleteGitHubOAuthCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CompleteGitHubOAuthCommand request, CancellationToken cancellationToken)
    {
        var token = await oauthService.ExchangeCodeAsync(request.Code, cancellationToken);
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
