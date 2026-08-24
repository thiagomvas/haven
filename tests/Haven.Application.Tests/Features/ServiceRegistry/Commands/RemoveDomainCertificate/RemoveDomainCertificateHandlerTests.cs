using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.ServiceRegistry.Commands.RemoveDomainCertificate;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceRegistry.Commands.RemoveDomainCertificate;

[Category("Unit")]
public sealed class RemoveDomainCertificateHandlerTests
{
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository;
    private IDomainCertificateRepository _domainCertificateRepository;
    private ITraefikDynamicConfigWriter _traefikDynamicConfigWriter;
    private RemoveDomainCertificateHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _domainCertificateRepository = Substitute.For<IDomainCertificateRepository>();
        _traefikDynamicConfigWriter = Substitute.For<ITraefikDynamicConfigWriter>();
        _traefikDynamicConfigWriter.RemoveDomainCertificateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _sut = new RemoveDomainCertificateHandler(
            _serviceRegistryEntryRepository, _domainCertificateRepository, _traefikDynamicConfigWriter);
    }

    [Test]
    public async Task Handle_EntryNotFound_ReturnsFailure()
    {
        var command = new RemoveDomainCertificateCommand { ServiceId = Guid.NewGuid(), DomainId = Guid.NewGuid() };
        _serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ValidDomain_RemovesCertificateFromRepositoryAndDisk()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("example.com", 80, TlsMode.Custom);
        var command = new RemoveDomainCertificateCommand { ServiceId = entry.ServiceId!.Value, DomainId = domain.Id };
        _serviceRegistryEntryRepository.GetForServiceAsync(entry.ServiceId!.Value, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _domainCertificateRepository.Received(1).RemoveByDomainIdAsync(domain.Id, Arg.Any<CancellationToken>());
        await _traefikDynamicConfigWriter.Received(1).RemoveDomainCertificateAsync(domain.Id, Arg.Any<CancellationToken>());
    }
}
