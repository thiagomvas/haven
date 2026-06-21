using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class ServiceRegistryTests
{
    private ServiceRegistry _sut = null!;
    private IServiceRegistryEntryRepository _repository = null!;
    private ILogger<ServiceRegistry> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IServiceRegistryEntryRepository>();
        _logger = Substitute.For<ILogger<ServiceRegistry>>();
        _sut = new ServiceRegistry(_repository, _logger);
    }

    [Test]
    public async Task EnsureServiceRegisteredAsync_WhenEntryAlreadyExists_ShouldReturnExistingEntry()
    {
        var serviceId = Guid.NewGuid();
        var existing = ServiceRegistryEntry.Create(serviceId);
        _repository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.EnsureServiceRegisteredAsync(serviceId, CancellationToken.None);

        result.ShouldBe(existing);
        await _repository.DidNotReceive().InsertAsync(Arg.Any<ServiceRegistryEntry>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureServiceRegisteredAsync_WhenEntryDoesNotExist_ShouldInsertAndReturnNewEntry()
    {
        var serviceId = Guid.NewGuid();
        _repository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        var result = await _sut.EnsureServiceRegisteredAsync(serviceId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ServiceId.ShouldBe(serviceId);
        await _repository.Received(1).InsertAsync(result, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureServiceRegisteredAsync_WhenEntryDoesNotExist_ShouldLogRegistration()
    {
        var serviceId = Guid.NewGuid();
        _repository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        await _sut.EnsureServiceRegisteredAsync(serviceId, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(serviceId.ToString())),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task EnsureServiceRegisteredAsync_WhenEntryAlreadyExists_ShouldNotLog()
    {
        var serviceId = Guid.NewGuid();
        _repository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(ServiceRegistryEntry.Create(serviceId));

        await _sut.EnsureServiceRegisteredAsync(serviceId, CancellationToken.None);

        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task GetForServiceAsync_ShouldDelegateToRepository()
    {
        var serviceId = Guid.NewGuid();
        var entry = ServiceRegistryEntry.Create(serviceId);
        _repository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _sut.GetForServiceAsync(serviceId, CancellationToken.None);

        result.ShouldBe(entry);
        await _repository.Received(1).GetForServiceAsync(serviceId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetForServiceAsync_WhenEntryNotFound_ShouldReturnNull()
    {
        _repository.GetForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        var result = await _sut.GetForServiceAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
    }
}
