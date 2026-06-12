using Hangfire;

using Haven.Application.Configuration;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class BackupSchedulerServiceTests
{
    private IRecurringJobManager _recurringJobManager = null!;
    private IOptionsMonitor<BackupOptions> _backupOptions = null!;
    private ILogger<BackupSchedulerService> _logger = null!;
    private BackupSchedulerService _sut = null!;

    private const string JobId = "automated-backup";

    [SetUp]
    public void Setup()
    {
        _recurringJobManager = Substitute.For<IRecurringJobManager>();
        _backupOptions = Substitute.For<IOptionsMonitor<BackupOptions>>();
        _logger = Substitute.For<ILogger<BackupSchedulerService>>();

        // IOptionsMonitor<T>.OnChange signature is Action<T, string?> — not Action<T>
        _backupOptions.OnChange(Arg.Any<Action<BackupOptions, string?>>())
            .Returns(Substitute.For<IDisposable>());

        _sut = new BackupSchedulerService(_recurringJobManager, _backupOptions, _logger);
    }

    [TearDown]
    public void TearDown() => _sut?.Dispose();

    // AddOrUpdate<T> is a Hangfire extension method and cannot be intercepted by NSubstitute.
    // For the "enabled" path the key assertion is that RemoveIfExists is NOT called.

    [Test(Description = "When backup is enabled StartAsync does not remove the recurring job")]
    public async Task StartAsync_WithBackupEnabled_DoesNotRemoveJob()
    {
        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            Enabled = true,
            CronExpression = "0 2 * * *"
        });

        await _sut.StartAsync(CancellationToken.None);

        _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    [Test(Description = "When backup is disabled StartAsync removes any existing recurring job by its well-known ID")]
    public async Task StartAsync_WithBackupDisabled_RemovesJobById()
    {
        _backupOptions.CurrentValue.Returns(new BackupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);

        _recurringJobManager.Received(1).RemoveIfExists(JobId);
    }

    [Test(Description = "StartAsync subscribes to options changes so schedule updates apply at runtime without restart")]
    public async Task StartAsync_RegistersOnChangeListener()
    {
        _backupOptions.CurrentValue.Returns(new BackupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);

        _backupOptions.Received(1).OnChange(Arg.Any<Action<BackupOptions, string?>>());
    }

    [Test(Description = "StopAsync disposes the options change listener to stop receiving updates")]
    public async Task StopAsync_DisposesOptionsChangeListener()
    {
        var listener = Substitute.For<IDisposable>();
        _backupOptions.OnChange(Arg.Any<Action<BackupOptions, string?>>()).Returns(listener);
        _backupOptions.CurrentValue.Returns(new BackupOptions { Enabled = false });

        _sut = new BackupSchedulerService(_recurringJobManager, _backupOptions, _logger);
        await _sut.StartAsync(CancellationToken.None);

        await _sut.StopAsync(CancellationToken.None);

        listener.Received(1).Dispose();
    }

    [Test(Description = "Calling Dispose directly also cleans up the options change listener")]
    public async Task Dispose_DisposesOptionsChangeListener()
    {
        var listener = Substitute.For<IDisposable>();
        _backupOptions.OnChange(Arg.Any<Action<BackupOptions, string?>>()).Returns(listener);
        _backupOptions.CurrentValue.Returns(new BackupOptions { Enabled = false });

        _sut = new BackupSchedulerService(_recurringJobManager, _backupOptions, _logger);
        await _sut.StartAsync(CancellationToken.None);

        _sut.Dispose();

        listener.Received(1).Dispose();
    }

    [Test(Description = "When options change to enabled the existing job is not removed")]
    public async Task OnOptionsChange_WithBackupEnabled_DoesNotRemoveJob()
    {
        Action<BackupOptions, string?>? capturedCallback = null;
        _backupOptions.OnChange(Arg.Do<Action<BackupOptions, string?>>(cb => capturedCallback = cb))
            .Returns(Substitute.For<IDisposable>());
        _backupOptions.CurrentValue.Returns(new BackupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);
        _recurringJobManager.ClearReceivedCalls();

        capturedCallback!.Invoke(new BackupOptions { Enabled = true, CronExpression = "0 3 * * *" }, null);

        _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    [Test(Description = "When options change to disabled the recurring job is removed by its well-known ID")]
    public async Task OnOptionsChange_WithBackupDisabled_RemovesJobById()
    {
        Action<BackupOptions, string?>? capturedCallback = null;
        _backupOptions.OnChange(Arg.Do<Action<BackupOptions, string?>>(cb => capturedCallback = cb))
            .Returns(Substitute.For<IDisposable>());
        _backupOptions.CurrentValue.Returns(new BackupOptions { Enabled = true, CronExpression = "0 0 * * *" });

        await _sut.StartAsync(CancellationToken.None);

        capturedCallback!.Invoke(new BackupOptions { Enabled = false }, null);

        _recurringJobManager.Received(1).RemoveIfExists(JobId);
    }

    [Test(Description = "StopAsync completes synchronously — it does not block or schedule async work")]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        _backupOptions.CurrentValue.Returns(new BackupOptions { Enabled = false });
        await _sut.StartAsync(CancellationToken.None);

        var task = _sut.StopAsync(CancellationToken.None);

        task.IsCompleted.ShouldBeTrue();
        await task;
    }
}
