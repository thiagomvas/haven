using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.GitCredentials.Queries.StartGitHubOAuth;

public sealed class StartGitHubOAuthHandler(IGitHubOAuthService oauthService, IOAuthStateStore stateStore)
    : IQueryHandler<StartGitHubOAuthQuery, string>
{
    public ValueTask<Result<string>> Handle(StartGitHubOAuthQuery query, CancellationToken cancellationToken)
    {
        var state = stateStore.GenerateState();
        var authorizeUrl = oauthService.BuildAuthorizeUrl(state);
        return new ValueTask<Result<string>>(Result<string>.Success(authorizeUrl));
    }
}
