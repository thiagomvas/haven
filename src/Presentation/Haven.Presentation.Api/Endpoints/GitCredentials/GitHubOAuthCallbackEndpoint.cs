using FastEndpoints;

using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Features.GitCredentials.Commands.CompleteGitHubOAuth;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.GitCredentials;

public sealed class GitHubOAuthCallbackRequest
{
    public string? Code { get; set; }
    public string? State { get; set; }
}

public sealed class GitHubOAuthCallbackEndpoint(IMediator mediator, IOAuthStateStore stateStore)
    : Endpoint<GitHubOAuthCallbackRequest>
{
    public override void Configure()
    {
        Get("/github/oauth/callback");
        AllowAnonymous();
        Options(x => x.WithTags("Git Credentials"));
        Summary(s =>
        {
            s.Summary = "GitHub App OAuth callback";
            s.Description = "Completes the GitHub App user-to-server OAuth flow and stores the resulting credentials.";
            s[302] = "Redirect back to the Haven web app";
        });
    }

    public override async Task HandleAsync(GitHubOAuthCallbackRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.Code) || string.IsNullOrEmpty(req.State) || !stateStore.TryConsumeState(req.State))
        {
            await Send.RedirectAsync("/git-providers?githubOAuth=error", allowRemoteRedirects: false);
            return;
        }

        var result = await mediator.Send(new CompleteGitHubOAuthCommand { Code = req.Code }, ct);

        await Send.RedirectAsync(
            result.IsSuccess ? "/git-providers?githubOAuth=success" : "/git-providers?githubOAuth=error",
            allowRemoteRedirects: false);
    }
}
