using FluentValidation;

namespace Haven.Application.Features.Backups.Commands.RestoreBackup;

public sealed class RestoreBackupValidator : AbstractValidator<RestoreBackupCommand>
{
    public RestoreBackupValidator()
    {
        When(x => x.Source == RestoreSource.FileSystem, () =>
            RuleFor(x => x.SnapshotName)
                .NotEmpty()
                .WithMessage("SnapshotName is required for file system restore."));

        When(x => x.Source == RestoreSource.Git, () =>
            RuleFor(x => x.CommitSha)
                .NotEmpty()
                .WithMessage("CommitSha is required for git restore."));
    }
}