using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.GitCredentials.Queries.StartGitHubOAuth;

using GitCredentialsEntity = Haven.Domain.Entities.GitCredentials;

public sealed class StartGitHubOAuthHandler(
    IGitHubOAuthService oauthService,
    IOAuthStateStore stateStore,
    IGitCredentialsRepository credentialsRepository)
    : IQueryHandler<StartGitHubOAuthQuery, string>
{
    public async ValueTask<Result<string>> Handle(StartGitHubOAuthQuery query, CancellationToken cancellationToken)
    {
        if (query.CredentialId.HasValue)
        {
            var credentials = await credentialsRepository.GetByIdAsync(query.CredentialId.Value, cancellationToken);
            if (credentials is null)
                return Error.NotFoundFor(nameof(GitCredentialsEntity), query.CredentialId.Value);

            if (credentials.ProviderType != GitProviderType.GitHub)
                return Error.Failed;
        }

        var state = stateStore.GenerateState(query.CredentialId);
        var authorizeUrl = oauthService.BuildAuthorizeUrl(state);
        return Result<string>.Success(authorizeUrl);
    }
}