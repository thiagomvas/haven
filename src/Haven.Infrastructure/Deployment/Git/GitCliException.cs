namespace Haven.Infrastructure.Deployment.Git;

public sealed class GitCliException : Exception
{
    public GitCliException() : base()
    {
    }

    public GitCliException(string? message) : base(message)
    {
    }

    public GitCliException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public GitCliException(string? message, int exitCode, string? standardError) : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError;
    }

    public int ExitCode { get; init; }
    public string StandardError { get; init; }
}