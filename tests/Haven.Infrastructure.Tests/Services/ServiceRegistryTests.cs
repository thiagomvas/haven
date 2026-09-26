using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Npgsql;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class ServiceRegistryTests
{
    private ServiceRegistry _sut = null!;
    private IServiceRegistryEntryRepository _repository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ILogger<ServiceRegistry> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IServiceRegistryEntryRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<ServiceRegistry>>();
        _sut = new ServiceRegistry(_repository, _unitOfWork, _logger);
    }

    private static PostgresException CreateUniqueViolation(string constraintName) =>
        new(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            detail: null, hint: null, position: 0, internalPosition: 0, internalQuery: null, where: null,
            schemaName: "public", tableName: "service_registry", columnName: null, dataTypeName: null,
            constraintName: constraintName, file: "index.c", line: "0", routine: "_bt_check_unique");

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
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Regression test: two concurrent deploy/start/restart operations for the same service both
    // see no existing registry entry and race to insert one. Postgres allows only the first
    // insert through the unique index on service_id and rejects the second with a
    // DbUpdateException (23505 on IX_service_registry_service_id) - this must be caught and
    // resolved by handing back the entry that actually landed, not left to bubble up and fail
    // the whole deploy/save.
    [Test]
    public async Task EnsureServiceRegisteredAsync_WhenConcurrentInsertWinsRace_ShouldReturnWinningEntry()
    {
        var serviceId = Guid.NewGuid();
        var winningEntry = ServiceRegistryEntry.Create(serviceId);

        _repository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null, winningEntry);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException("conflict", CreateUniqueViolation("IX_service_registry_service_id")));

        var result = await _sut.EnsureServiceRegisteredAsync(serviceId, CancellationToken.None);

        result.ShouldBe(winningEntry);
        _unitOfWork.Received(1).Detach(Arg.Any<ServiceRegistryEntry>());
        await _repository.Received(2).GetForServiceAsync(serviceId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureSidecarRegisteredAsync_WhenConcurrentInsertWinsRace_ShouldReturnWinningEntry()
    {
        var sidecarId = Guid.NewGuid();
        var winningEntry = ServiceRegistryEntry.CreateForSidecar(sidecarId);

        _repository.GetForSidecarAsync(sidecarId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null, winningEntry);

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException("conflict", CreateUniqueViolation("IX_service_registry_sidecar_id")));

        var result = await _sut.EnsureSidecarRegisteredAsync(sidecarId, CancellationToken.None);

        result.ShouldBe(winningEntry);
        _unitOfWork.Received(1).Detach(Arg.Any<ServiceRegistryEntry>());
        await _repository.Received(2).GetForSidecarAsync(sidecarId, Arg.Any<CancellationToken>());
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