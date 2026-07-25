using Haven.Infrastructure.Backup;

using Shouldly;

namespace Haven.Infrastructure.Tests.Backup;

[TestFixture]
[Category("Unit")]
public sealed class BackupCoordinationLockTests
{
    [Test(Description = "A second concurrent acquire attempt fails fast while the first is still held")]
    public void TryAcquire_WhileAlreadyHeld_ReturnsFalse()
    {
        var sut = new BackupCoordinationLock();

        sut.TryAcquire(out var firstRelease).ShouldBeTrue();
        sut.TryAcquire(out _).ShouldBeFalse();

        firstRelease.Dispose();
    }

    [Test(Description = "Once the holder releases the lock, a subsequent acquire attempt succeeds")]
    public void TryAcquire_AfterRelease_SucceedsAgain()
    {
        var sut = new BackupCoordinationLock();

        sut.TryAcquire(out var firstRelease).ShouldBeTrue();
        firstRelease.Dispose();

        sut.TryAcquire(out var secondRelease).ShouldBeTrue();
        secondRelease.Dispose();
    }
}
