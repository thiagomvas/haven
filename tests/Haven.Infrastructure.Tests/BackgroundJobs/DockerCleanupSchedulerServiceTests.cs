using Hangfire;

using Haven.Application.Configuration;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class DockerCleanupSchedulerServiceTests
{
    private IRecurringJobManager _recurringJobManager = null!;
    private IOptionsMonitor<DockerCleanupOptions> _dockerCleanupOptions = null!;
    private ILogger<DockerCleanupSchedulerService> _logger = null!;
    private DockerCleanupSchedulerService _sut = null!;

    private const string JobId = "docker-cleanup";

    [SetUp]
    public void Setup()
    {
        _recurringJobManager = Substitute.For<IRecurringJobManager>();
        _dockerCleanupOptions = Substitute.For<IOptionsMonitor<DockerCleanupOptions>>();
        _logger = Substitute.For<ILogger<DockerCleanupSchedulerService>>();

        // IOptionsMonitor<T>.OnChange signature is Action<T, string?> — not Action<T>
        _dockerCleanupOptions.OnChange(Arg.Any<Action<DockerCleanupOptions, string?>>())
            .Returns(Substitute.For<IDisposable>());

        _sut = new DockerCleanupSchedulerService(_recurringJobManager, _dockerCleanupOptions, _logger);
    }

    [TearDown]
    public void TearDown() => _sut?.Dispose();

    // AddOrUpdate<T> is a Hangfire extension method and cannot be intercepted by NSubstitute.
    // For the "enabled" path the key assertion is that RemoveIfExists is NOT called.

    [Test(Description = "When Docker cleanup is enabled StartAsync does not remove the recurring job")]
    public async Task StartAsync_WithDockerCleanupEnabled_DoesNotRemoveJob()
    {
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions
        {
            Enabled = true,
            CronExpression = "0 3 * * *"
        });

        await _sut.StartAsync(CancellationToken.None);

        _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    [Test(Description = "When Docker cleanup is disabled StartAsync removes any existing recurring job by its well-known ID")]
    public async Task StartAsync_WithDockerCleanupDisabled_RemovesJobById()
    {
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);

        _recurringJobManager.Received(1).RemoveIfExists(JobId);
    }

    [Test(Description = "StartAsync subscribes to options changes so schedule updates apply at runtime without restart")]
    public async Task StartAsync_RegistersOnChangeListener()
    {
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);

        _dockerCleanupOptions.Received(1).OnChange(Arg.Any<Action<DockerCleanupOptions, string?>>());
    }

    [Test(Description = "StopAsync disposes the options change listener to stop receiving updates")]
    public async Task StopAsync_DisposesOptionsChangeListener()
    {
        var listener = Substitute.For<IDisposable>();
        _dockerCleanupOptions.OnChange(Arg.Any<Action<DockerCleanupOptions, string?>>()).Returns(listener);
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions { Enabled = false });

        _sut = new DockerCleanupSchedulerService(_recurringJobManager, _dockerCleanupOptions, _logger);
        await _sut.StartAsync(CancellationToken.None);

        await _sut.StopAsync(CancellationToken.None);

        listener.Received(1).Dispose();
    }

    [Test(Description = "Calling Dispose directly also cleans up the options change listener")]
    public async Task Dispose_DisposesOptionsChangeListener()
    {
        var listener = Substitute.For<IDisposable>();
        _dockerCleanupOptions.OnChange(Arg.Any<Action<DockerCleanupOptions, string?>>()).Returns(listener);
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions { Enabled = false });

        _sut = new DockerCleanupSchedulerService(_recurringJobManager, _dockerCleanupOptions, _logger);
        await _sut.StartAsync(CancellationToken.None);

        _sut.Dispose();

        listener.Received(1).Dispose();
    }

    [Test(Description = "When options change to enabled the existing job is not removed")]
    public async Task OnOptionsChange_WithDockerCleanupEnabled_DoesNotRemoveJob()
    {
        Action<DockerCleanupOptions, string?>? capturedCallback = null;
        _dockerCleanupOptions.OnChange(Arg.Do<Action<DockerCleanupOptions, string?>>(cb => capturedCallback = cb))
            .Returns(Substitute.For<IDisposable>());
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions { Enabled = false });

        await _sut.StartAsync(CancellationToken.None);
        _recurringJobManager.ClearReceivedCalls();

        capturedCallback!.Invoke(new DockerCleanupOptions { Enabled = true, CronExpression = "0 4 * * *" }, null);

        _recurringJobManager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    [Test(Description = "When options change to disabled the recurring job is removed by its well-known ID")]
    public async Task OnOptionsChange_WithDockerCleanupDisabled_RemovesJobById()
    {
        Action<DockerCleanupOptions, string?>? capturedCallback = null;
        _dockerCleanupOptions.OnChange(Arg.Do<Action<DockerCleanupOptions, string?>>(cb => capturedCallback = cb))
            .Returns(Substitute.For<IDisposable>());
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions { Enabled = true, CronExpression = "0 0 * * *" });

        await _sut.StartAsync(CancellationToken.None);

        capturedCallback!.Invoke(new DockerCleanupOptions { Enabled = false }, null);

        _recurringJobManager.Received(1).RemoveIfExists(JobId);
    }

    [Test(Description = "StopAsync completes synchronously — it does not block or schedule async work")]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        _dockerCleanupOptions.CurrentValue.Returns(new DockerCleanupOptions { Enabled = false });
        await _sut.StartAsync(CancellationToken.None);

        var task = _sut.StopAsync(CancellationToken.None);

        task.IsCompleted.ShouldBeTrue();
        await task;
    }
}