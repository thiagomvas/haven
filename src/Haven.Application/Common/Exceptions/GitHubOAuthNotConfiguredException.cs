using Haven.Domain.Exceptions;

namespace Haven.Application.Common.Exceptions;

public sealed class GitHubOAuthNotConfiguredException : HavenException
{
    public GitHubOAuthNotConfiguredException()
        : base("GitHub OAuth is not configured. An administrator must set the GitHub App Client ID and Secret in Settings.")
    {
    }

    private GitHubOAuthNotConfiguredException(string message) : base(message)
    {
    }

    private GitHubOAuthNotConfiguredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}