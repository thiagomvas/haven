using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.GitCredentials.Queries.StartGitHubOAuth;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.GitCredentials;

public sealed class GitHubOAuthAuthorizeEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<string>>
{
    public override void Configure()
    {
        Get("/github/oauth/authorize");
        Options(x => x.WithTags("Git Credentials"));
        Summary(s =>
        {
            s.Summary = "Start GitHub App OAuth flow";
            s.Description = "Returns the GitHub authorize URL to navigate the browser to, starting the GitHub App user-to-server OAuth flow. Pass ?credentialId= to reconnect an existing GitHub credential instead of creating a new one.";
            s[200] = "Authorize URL";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var credentialId = Query<Guid?>("credentialId", isRequired: false);

        var result = await mediator.Send(new StartGitHubOAuthQuery { CredentialId = credentialId }, ct);
        await this.SendResultAsync(result, ct);
    }
}
