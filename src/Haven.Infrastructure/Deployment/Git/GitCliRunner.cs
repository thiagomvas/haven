using System.Diagnostics;

namespace Haven.Infrastructure.Deployment.Git;

/// <summary>
/// Shells out to the system `git` binary for operations that need SSH transport, since LibGit2Sharp's
/// bundled native binaries have no SSH support.
/// </summary>
public static class GitCliRunner
{
    public static async Task<string> RunAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        string? sshKeyPath,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        if (sshKeyPath is not null)
        {
            startInfo.Environment["GIT_SSH_COMMAND"] =
                $"ssh -i \"{sshKeyPath}\" -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new -o BatchMode=yes";
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        if (process.ExitCode != 0)
        {
            throw new GitCliException(
                $"git {string.Join(' ', arguments)} failed with exit code {process.ExitCode}: {stdErr}",
                process.ExitCode,
                stdErr);
        }

        return stdOut;
    }
}