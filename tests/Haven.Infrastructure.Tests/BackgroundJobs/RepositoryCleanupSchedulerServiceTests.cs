using Hangfire;

using Haven.Application.Configuration;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class RepositoryCleanupSchedulerServiceTests
{
    private IRecurringJobManager _recurringJobManager = null!;
    private IOptionsMonitor<RepositoryCleanupOptions> _repositoryCleanupOptions = null!;
    private ILogger<RepositoryCleanupSchedulerService> _logger = null!;
    private RepositoryCleanupSchedulerService _sut = null!;

    private const string JobId = "repository-cleanup";

    [SetUp]
    public void Setup()
    {
        _recurringJobManager = Substitute.For<IRecurringJobManager>();
        _repositoryCleanupOptions = Substitute.For<IOptionsMonitor<RepositoryCleanupOptions>>();
        _logger = Substitute.For<ILogger<RepositoryCleanupSchedulerService>>();

        // IOptionsMonitor<T>.OnChange signature is Action<T, string?> — not Action<T>
        _repositoryCleanupOptions.OnChange(Arg.Any<Action<RepositoryCleanupOptions, string?>>())
            .Returns(Substitute.For<IDisposable>());

        _sut = new RepositoryCleanupSchedulerService(_recurringJobManager, _repositoryCleanupOptions, _logger);
    }

    [TearDown]
    public void TearDown() => _sut?.Dispose();

    // AddOrUpdate<T> is a Hangfire extension method and cannot be intercepted by NSubstitute.
    // For the "enabled" path the key assertion is that RemoveIfExists is NOT called.

    [Test(Description = "When repository cleanup is enabled StartAsync does not remove the recurring job")]
    public async Task StartAsync_WithRepositoryCleanupEnabled_DoesNotRemoveJob()
    {
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions
        {
            Enabled = true,
            CronExpression = "0 4 * * *"
        });

        await _sut.StartAsync(CancellationToken.None);

        _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    [Test(Description = "When repository cleanup is disabled StartAsync removes any existing recurring job by its well-known ID")]
    public async Task StartAsync_WithRepositoryCleanupDisabled_RemovesJobById()
    {
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);

        _recurringJobManager.Received(1).RemoveIfExists(JobId);
    }

    [Test(Description = "StartAsync subscribes to options changes so schedule updates apply at runtime without restart")]
    public async Task StartAsync_RegistersOnChangeListener()
    {
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);

        _repositoryCleanupOptions.Received(1).OnChange(Arg.Any<Action<RepositoryCleanupOptions, string?>>());
    }

    [Test(Description = "StopAsync disposes the options change listener to stop receiving updates")]
    public async Task StopAsync_DisposesOptionsChangeListener()
    {
        var listener = Substitute.For<IDisposable>();
        _repositoryCleanupOptions.OnChange(Arg.Any<Action<RepositoryCleanupOptions, string?>>()).Returns(listener);
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions { Enabled = false });

        _sut = new RepositoryCleanupSchedulerService(_recurringJobManager, _repositoryCleanupOptions, _logger);
        await _sut.StartAsync(CancellationToken.None);

        await _sut.StopAsync(CancellationToken.None);

        listener.Received(1).Dispose();
    }

    [Test(Description = "Calling Dispose directly also cleans up the options change listener")]
    public async Task Dispose_DisposesOptionsChangeListener()
    {
        var listener = Substitute.For<IDisposable>();
        _repositoryCleanupOptions.OnChange(Arg.Any<Action<RepositoryCleanupOptions, string?>>()).Returns(listener);
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions { Enabled = false });

        _sut = new RepositoryCleanupSchedulerService(_recurringJobManager, _repositoryCleanupOptions, _logger);
        await _sut.StartAsync(CancellationToken.None);

        _sut.Dispose();

        listener.Received(1).Dispose();
    }

    [Test(Description = "When options change to enabled the existing job is not removed")]
    public async Task OnOptionsChange_WithRepositoryCleanupEnabled_DoesNotRemoveJob()
    {
        Action<RepositoryCleanupOptions, string?>? capturedCallback = null;
        _repositoryCleanupOptions.OnChange(Arg.Do<Action<RepositoryCleanupOptions, string?>>(cb => capturedCallback = cb))
            .Returns(Substitute.For<IDisposable>());
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);
        _recurringJobManager.ClearReceivedCalls();

        capturedCallback!.Invoke(new RepositoryCleanupOptions { Enabled = true, CronExpression = "0 5 * * *" }, null);

        _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    [Test(Description = "When options change to disabled the recurring job is removed by its well-known ID")]
    public async Task OnOptionsChange_WithRepositoryCleanupDisabled_RemovesJobById()
    {
        Action<RepositoryCleanupOptions, string?>? capturedCallback = null;
        _repositoryCleanupOptions.OnChange(Arg.Do<Action<RepositoryCleanupOptions, string?>>(cb => capturedCallback = cb))
            .Returns(Substitute.For<IDisposable>());
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions { Enabled = true, CronExpression = "0 0 * * *" });

        await _sut.StartAsync(CancellationToken.None);

        capturedCallback!.Invoke(new RepositoryCleanupOptions { Enabled = false }, null);

        _recurringJobManager.Received(1).RemoveIfExists(JobId);
    }

    [Test(Description = "StopAsync completes synchronously — it does not block or schedule async work")]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        _repositoryCleanupOptions.CurrentValue.Returns(new RepositoryCleanupOptions { Enabled = false });
        await _sut.StartAsync(CancellationToken.None);

        var task = _sut.StopAsync(CancellationToken.None);

        task.IsCompleted.ShouldBeTrue();
        await task;
    }
}
