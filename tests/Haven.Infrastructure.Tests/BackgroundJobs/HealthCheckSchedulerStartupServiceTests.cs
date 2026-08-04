using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class HealthCheckSchedulerStartupServiceTests
{
    private IServiceScopeFactory _scopeFactory = null!;
    private IServiceScope _scope = null!;
    private IServiceProvider _serviceProvider = null!;
    private IHealthCheckRepository _healthCheckRepository = null!;
    private IHealthCheckScheduler _scheduler = null!;
    private HealthCheckSchedulerStartupService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scope = Substitute.For<IServiceScope>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _healthCheckRepository = Substitute.For<IHealthCheckRepository>();
        _scheduler = Substitute.For<IHealthCheckScheduler>();

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IHealthCheckRepository)).Returns(_healthCheckRepository);
        _serviceProvider.GetService(typeof(IHealthCheckScheduler)).Returns(_scheduler);

        _sut = new HealthCheckSchedulerStartupService(_scopeFactory, Substitute.For<ILogger<HealthCheckSchedulerStartupService>>());
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
    }

    [Test]
    public async Task StartAsync_WhenNoHealthChecksExist_ShouldNotScheduleAnything()
    {
        _healthCheckRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<HealthCheck>());

        await _sut.StartAsync(CancellationToken.None);

        _scheduler.DidNotReceiveWithAnyArgs().Schedule(default!);
    }

    [Test]
    public async Task StartAsync_WhenHealthChecksExist_ShouldScheduleEachOne()
    {
        var serviceId = Guid.NewGuid();
        var healthChecks = new List<HealthCheck>
        {
            HealthCheck.Create(serviceId, "check-1", HealthCheckKind.Http, true, "* * * * *", "{}"),
            HealthCheck.Create(serviceId, "check-2", HealthCheckKind.Bash, true, "* * * * *", "{}")
        };

        _healthCheckRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(healthChecks);

        await _sut.StartAsync(CancellationToken.None);

        _scheduler.Received(1).Schedule(healthChecks[0]);
        _scheduler.Received(1).Schedule(healthChecks[1]);
        _scheduler.Received(2).Schedule(Arg.Any<HealthCheck>());
    }

    [Test]
    public async Task StartAsync_ShouldUseScopedServiceProvider()
    {
        _healthCheckRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<HealthCheck>());

        await _sut.StartAsync(CancellationToken.None);

        _scopeFactory.Received(1).CreateScope();
    }

    [Test]
    public async Task StopAsync_ShouldCompleteWithoutError()
    {
        await _sut.StopAsync(CancellationToken.None);
    }
}
