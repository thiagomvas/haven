namespace Haven.Application.Common.Interfaces;

/// <summary>
/// Serializes backup, restore, and manifest-sync operations so two of them never write to
/// the same manifests/backups directories at the same time.
/// </summary>
public interface IBackupCoordinationLock
{
    /// <summary>
    /// Attempts to acquire the lock without waiting. Returns <c>false</c> immediately if another
    /// operation already holds it; the caller should fail fast rather than queue behind it.
    /// </summary>
    bool TryAcquire(out IDisposable release);
}