using Haven.Application.Common.Interfaces.Services;
using Haven.Infrastructure.BackgroundJobs;

using NSubstitute;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class HealthCheckJobTests
{
    [Test]
    public async Task ExecuteAsync_ShouldDelegateToExecutor()
    {
        var executor = Substitute.For<IHealthCheckExecutor>();
        var sut = new HealthCheckJob(executor);
        var healthCheckId = Guid.NewGuid();

        await sut.ExecuteAsync(healthCheckId, CancellationToken.None);

        await executor.Received(1).ExecuteAsync(healthCheckId, Arg.Any<CancellationToken>());
    }
}
