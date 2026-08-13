using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.ServiceRegistry.Commands.DeleteDomain;
using Haven.Domain.Aggregates;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceRegistry.Commands.DeleteDomain;

[Category("Unit")]
public sealed class DeleteDomainHandlerTests
{
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository;
    private DeleteDomainHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _sut = new DeleteDomainHandler(_serviceRegistryEntryRepository);
    }

    [Test]
    public async Task Handle_EntryNotFound_ReturnsFailure()
    {
        var serviceId = Guid.NewGuid();
        var command = new DeleteDomainCommand { ServiceId = serviceId, DomainId = Guid.NewGuid() };
        _serviceRegistryEntryRepository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_DomainNotFound_ReturnsFailure()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var command = new DeleteDomainCommand { ServiceId = entry.ServiceId, DomainId = Guid.NewGuid() };
        _serviceRegistryEntryRepository.GetForServiceAsync(entry.ServiceId, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ValidDomain_RemovesDomain()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("example.com", 8080);
        var command = new DeleteDomainCommand { ServiceId = entry.ServiceId, DomainId = domain.Id };
        _serviceRegistryEntryRepository.GetForServiceAsync(entry.ServiceId, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        entry.Domains.ShouldBeEmpty();
    }
}
