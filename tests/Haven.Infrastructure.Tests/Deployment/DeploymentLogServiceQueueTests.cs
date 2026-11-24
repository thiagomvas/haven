using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Hubs;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Domain.Entities;
using DeploymentEntity = Haven.Domain.Entities.Deployment;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[TestFixture]
public class DeploymentLogServiceQueueTests
{
    private IDeploymentRepository _repository = null!;
    private DeploymentLogService _sut = null!;
    private readonly Guid _serviceId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IDeploymentRepository>();
        var services = new ServiceCollection();
        services.AddSingleton(_repository);
        services.AddSingleton(Substitute.For<IUnitOfWork>());
        services.AddSingleton(Substitute.For<IServiceStatusPublisher>());

        var options = Substitute.For<IOptionsMonitor<InstanceOptions>>();
        options.CurrentValue.Returns(new InstanceOptions());

        _sut = new DeploymentLogService(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<IDeploymentLogNotifier>(),
            options);
    }

    [Test]
    public async Task Queue_WhenNoActiveDeployment_ShouldCreateNew()
    {
        _repository.FindActiveForServiceAsync(_serviceId, Arg.Any<CancellationToken>())
            .Returns((DeploymentEntity?)null);

        var result = await _sut.QueueDeploymentForServiceAsync(_serviceId, CancellationToken.None);

        result.Created.ShouldBeTrue();
        await _repository.Received(1).AddAsync(result.Deployment, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Queue_WhenRecentQueuedDeploymentExists_ShouldReturnItWithoutCreating()
    {
        var existing = DeploymentEntity.Create(_serviceId, "log");
        _repository.FindActiveForServiceAsync(_serviceId, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.QueueDeploymentForServiceAsync(_serviceId, CancellationToken.None);

        result.Created.ShouldBeFalse();
        result.Deployment.ShouldBeSameAs(existing);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Test]
    public async Task Queue_WhenQueuedDeploymentIsStale_ShouldFailItAndCreateNew()
    {
        var stale = DeploymentEntity.Create(_serviceId, "log");
        stale.StartedAt = DateTimeOffset.UtcNow - DeploymentLogService.StaleQueuedThreshold - TimeSpan.FromMinutes(1);
        _repository.FindActiveForServiceAsync(_serviceId, Arg.Any<CancellationToken>()).Returns(stale);

        var result = await _sut.QueueDeploymentForServiceAsync(_serviceId, CancellationToken.None);

        result.Created.ShouldBeTrue();
        result.Deployment.ShouldNotBeSameAs(stale);
        stale.Status.ShouldBe(DeploymentStatus.Failed);
    }

    [Test]
    public async Task Queue_WhenDeploymentAlreadyRunning_ShouldNotTreatAsStale()
    {
        var running = DeploymentEntity.Create(_serviceId, "log");
        running.StartBuilding();
        running.StartedAt = DateTimeOffset.UtcNow - TimeSpan.FromHours(2);
        _repository.FindActiveForServiceAsync(_serviceId, Arg.Any<CancellationToken>()).Returns(running);

        var result = await _sut.QueueDeploymentForServiceAsync(_serviceId, CancellationToken.None);

        result.Created.ShouldBeFalse();
    }
}
