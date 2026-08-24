using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.ServiceRegistry.Commands.UpdateDomain;
using Haven.Domain.Aggregates;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceRegistry.Commands.UpdateDomain;

[Category("Unit")]
public sealed class UpdateDomainHandlerTests
{
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository;
    private IDomainCertificateRepository _domainCertificateRepository;
    private ITraefikDynamicConfigWriter _traefikDynamicConfigWriter;
    private UpdateDomainHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _domainCertificateRepository = Substitute.For<IDomainCertificateRepository>();
        _traefikDynamicConfigWriter = Substitute.For<ITraefikDynamicConfigWriter>();
        _traefikDynamicConfigWriter.RemoveDomainCertificateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _sut = new UpdateDomainHandler(_serviceRegistryEntryRepository, _domainCertificateRepository, _traefikDynamicConfigWriter);
    }

    [Test]
    public async Task Handle_EntryNotFound_ReturnsFailure()
    {
        var serviceId = Guid.NewGuid();
        var command = new UpdateDomainCommand { ServiceId = serviceId, DomainId = Guid.NewGuid(), Hostname = "new.com" };
        _serviceRegistryEntryRepository.GetForServiceAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_DomainNotFound_ReturnsFailure()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var command = new UpdateDomainCommand { ServiceId = entry.ServiceId, DomainId = Guid.NewGuid(), Hostname = "new.com" };
        _serviceRegistryEntryRepository.GetForServiceAsync(entry.ServiceId, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_HostnameCollidesWithAnotherEntry_ReturnsConflict_ExcludingSelf()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("old.com", 8080);
        var command = new UpdateDomainCommand { ServiceId = entry.ServiceId, DomainId = domain.Id, Hostname = "taken.com" };
        _serviceRegistryEntryRepository.GetForServiceAsync(entry.ServiceId, Arg.Any<CancellationToken>())
            .Returns(entry);
        _serviceRegistryEntryRepository.HostnameExistsAsync("taken.com", domain.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CONFLICT");
        domain.Hostname.ShouldBe("old.com");
    }

    [Test]
    public async Task Handle_ValidUpdate_UpdatesDomain()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("old.com", 8080);
        var command = new UpdateDomainCommand { ServiceId = entry.ServiceId, DomainId = domain.Id, Hostname = "new.com", ContainerPort = 3000 };
        _serviceRegistryEntryRepository.GetForServiceAsync(entry.ServiceId, Arg.Any<CancellationToken>())
            .Returns(entry);
        _serviceRegistryEntryRepository.HostnameExistsAsync("new.com", domain.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        domain.Hostname.ShouldBe("new.com");
        domain.ContainerPort.ShouldBe(3000);
    }
}