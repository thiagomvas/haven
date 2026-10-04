using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.CustomActions.Services;

public sealed class ExecActionStrategy(IContainerExecService execService, ILogger<ExecActionStrategy> logger)
    : IActionStrategy
{
    public bool CanHandle(CustomAction action) => action.Config is ExecActionConfig;

    public async Task<Result> ExecuteAsync(CustomAction action, CancellationToken ct)
    {
        if (action.Config is not ExecActionConfig config)
            return Error.NotSupported;

        if (config.Command.Count == 0)
            return Error.Validation("Exec action requires a command.");

        // With a shell the command parts are joined into a single script; otherwise they are passed as argv.
        IReadOnlyList<string> command = config.Shell is { } shell
            ? [ShellPath(shell), "-c", string.Join(' ', config.Command)]
            : config.Command;

        try
        {
            var result = await execService.ExecAsync(action.ServiceId, command, config.WorkingDir, config.User,
                action.Timeout, ct);
            if (result.IsFailure)
                return result.Error;

            var exec = result.Value;
            if (exec.ExitCode == 0)
                return Result.Success();

            logger.LogWarning(
                "Exec action {ActionId} ({ActionName}) exited with code {ExitCode}. StdErr: {StdErr}",
                action.Id, action.ActionName, exec.ExitCode, exec.StdErr);
            return Error.Docker.OperationFailed(
                $"Command exited with code {exec.ExitCode}.{(string.IsNullOrWhiteSpace(exec.StdErr) ? "" : $" {exec.StdErr.Trim()}")}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Error.CancelledOperation;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Exec action {ActionId} ({ActionName}) timed out after {Timeout}",
                action.Id, action.ActionName, action.Timeout);
            return Error.Docker.OperationFailed($"Command timed out after {action.Timeout}.");
        }
    }

    private static string ShellPath(ShellType shell) => shell switch
    {
        ShellType.Bash => "/bin/bash",
        ShellType.Sh => "/bin/sh",
        _ => throw new ArgumentOutOfRangeException(nameof(shell), shell, "Unsupported shell type.")
    };
}
