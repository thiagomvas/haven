namespace Haven.Application.Features.Backups.Commands.RestoreBackup;

public sealed record RestoreBackupResult
{
    public required bool DryRun { get; init; }
    public required EntityChangeSummary<ProjectRestoreItem> Projects { get; init; }
    public EntityChangeSummary<EnvironmentRestoreItem> Environments { get; init; } = new();
    public EntityChangeSummary<NetworkRestoreItem> Networks { get; init; } = new();
    public EntityChangeSummary<ServiceRestoreItem> Services { get; init; } = new();
    public EntityChangeSummary<EnvVarRestoreItem> EnvironmentVariables { get; init; } = new();
    public EntityChangeSummary<VolumeFileRestoreItem> VolumeFiles { get; init; } = new();

    /// <summary>
    /// Human-readable messages describing managed-volume files that failed to restore on disk.
    /// The DB/manifest changes for this restore had already been committed by the time volume
    /// files are restored, so a failure here is reported here rather than failing the whole
    /// restore - the caller should check this list to see if any files need manual attention.
    /// </summary>
    public IReadOnlyList<string> VolumeFileRestoreWarnings { get; init; } = [];
}

public sealed record EntityChangeSummary<T>
{
    public IReadOnlyList<T> Created { get; init; } = [];
    public IReadOnlyList<T> Updated { get; init; } = [];
    public IReadOnlyList<T> Deleted { get; init; } = [];
}

public sealed record ProjectRestoreItem(Guid Id, string Name);
public sealed record EnvironmentRestoreItem(Guid Id, string Name, Guid ProjectId, string? ProjectName = null);
public sealed record NetworkRestoreItem(Guid Id, string Name);
public sealed record ServiceRestoreItem(Guid Id, string Name, Guid EnvironmentId, Guid ProjectId, string? EnvironmentName = null, string? ProjectName = null);
public sealed record EnvVarRestoreItem(string Key, Guid ParentId, string? ParentName = null);
public sealed record VolumeFileRestoreItem(string Path, Guid ServiceId, string VolumeName, string? ServiceName = null);