using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.ServiceRegistry.Commands.DetachDomainCertificate;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceRegistry.Commands.DetachDomainCertificate;

[Category("Unit")]
public sealed class DetachDomainCertificateHandlerTests
{
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository;
    private ITraefikDynamicConfigWriter _traefikDynamicConfigWriter;
    private DetachDomainCertificateHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _traefikDynamicConfigWriter = Substitute.For<ITraefikDynamicConfigWriter>();
        _traefikDynamicConfigWriter.RemoveDomainCertificateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _sut = new DetachDomainCertificateHandler(_serviceRegistryEntryRepository, _traefikDynamicConfigWriter);
    }

    [Test]
    public async Task Handle_EntryNotFound_ReturnsFailure()
    {
        var command = new DetachDomainCertificateCommand { ServiceId = Guid.NewGuid(), DomainId = Guid.NewGuid() };
        _serviceRegistryEntryRepository.GetForServiceAsync(command.ServiceId, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ValidDomain_ClearsCertificateAndRemovesFromDisk()
    {
        var entry = ServiceRegistryEntry.Create(Guid.NewGuid());
        var domain = entry.AddDomain("example.com", 80, TlsMode.Custom);
        domain.SslCertificateId = Guid.NewGuid();
        var command = new DetachDomainCertificateCommand { ServiceId = entry.ServiceId!.Value, DomainId = domain.Id };
        _serviceRegistryEntryRepository.GetForServiceAsync(entry.ServiceId!.Value, Arg.Any<CancellationToken>())
            .Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        domain.SslCertificateId.ShouldBeNull();
        domain.Certificate.ShouldBeNull();
        await _traefikDynamicConfigWriter.Received(1).RemoveDomainCertificateAsync(domain.Id, Arg.Any<CancellationToken>());
    }
}
