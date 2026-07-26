using Haven.Application.Common.Interfaces;

namespace Haven.Infrastructure.Backup;

public sealed class BackupCoordinationLock : IBackupCoordinationLock, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryAcquire(out IDisposable release)
    {
        if (!_semaphore.Wait(0))
        {
            release = null!;
            return false;
        }

        release = new Releaser(_semaphore);
        return true;
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}