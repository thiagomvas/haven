using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.ServiceRegistry.Commands.AddDomain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.ServiceRegistry.Commands.AddDomain;

[Category("Unit")]
public sealed class AddDomainHandlerTests
{
    private IServiceRepository _serviceRepository;
    private ISidecarRepository _sidecarRepository;
    private IServiceRegistry _serviceRegistry;
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository;
    private AddDomainHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _sidecarRepository = Substitute.For<ISidecarRepository>();
        _serviceRegistry = Substitute.For<IServiceRegistry>();
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _sut = new AddDomainHandler(_serviceRepository, _sidecarRepository, _serviceRegistry, _serviceRegistryEntryRepository);
    }

    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public async Task Handle_ServiceNotFound_ReturnsFailure()
    {
        var command = new AddDomainCommand { ServiceId = Guid.NewGuid(), Hostname = "example.com", ContainerPort = 8080 };
        _serviceRepository.GetByIdAsync(command.ServiceId.Value, Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _serviceRegistry.DidNotReceiveWithAnyArgs().EnsureServiceRegisteredAsync(default, default);
    }

    [Test]
    public async Task Handle_HostnameAlreadyExists_ReturnsConflict()
    {
        var service = NewService();
        var command = new AddDomainCommand { ServiceId = service.Id, Hostname = "Example.com", ContainerPort = 8080 };
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _serviceRegistryEntryRepository.HostnameExistsAsync("example.com", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CONFLICT");
        await _serviceRegistry.DidNotReceiveWithAnyArgs().EnsureServiceRegisteredAsync(default, default);
    }

    [Test]
    public async Task Handle_ValidDomain_CreatesRegistryEntryAndAddsDomain()
    {
        var service = NewService();
        var entry = ServiceRegistryEntry.Create(service.Id);
        var command = new AddDomainCommand { ServiceId = service.Id, Hostname = "example.com", ContainerPort = 8080 };
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _serviceRegistryEntryRepository.HostnameExistsAsync("example.com", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _serviceRegistry.EnsureServiceRegisteredAsync(service.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        entry.Domains.Count.ShouldBe(1);
        entry.Domains.First().Hostname.ShouldBe("example.com");
    }

    [Test]
    public async Task Handle_SidecarNotFound_ReturnsFailure()
    {
        var command = new AddDomainCommand { SidecarId = Guid.NewGuid(), Hostname = "example.com", ContainerPort = 8080 };
        _serviceRegistryEntryRepository.HostnameExistsAsync("example.com", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _sidecarRepository.GetByIdAsync(command.SidecarId.Value, Arg.Any<CancellationToken>()).Returns((Sidecar?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _serviceRegistry.DidNotReceiveWithAnyArgs().EnsureSidecarRegisteredAsync(default, default);
    }

    [Test]
    public async Task Handle_SidecarNotTraefik_ReturnsFailure()
    {
        var sidecar = Sidecar.Create("whoami", SidecarKind.Whoami);
        var command = new AddDomainCommand { SidecarId = sidecar.Id, Hostname = "example.com", ContainerPort = 8080 };
        _serviceRegistryEntryRepository.HostnameExistsAsync("example.com", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _sidecarRepository.GetByIdAsync(sidecar.Id, Arg.Any<CancellationToken>()).Returns(sidecar);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _serviceRegistry.DidNotReceiveWithAnyArgs().EnsureSidecarRegisteredAsync(default, default);
    }

    [Test]
    public async Task Handle_ValidTraefikSidecarDomain_CreatesRegistryEntryAndAddsDomain()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik);
        var entry = ServiceRegistryEntry.CreateForSidecar(sidecar.Id);
        var command = new AddDomainCommand { SidecarId = sidecar.Id, Hostname = "traefik.example.com", ContainerPort = 8080 };
        _serviceRegistryEntryRepository.HostnameExistsAsync("traefik.example.com", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _sidecarRepository.GetByIdAsync(sidecar.Id, Arg.Any<CancellationToken>()).Returns(sidecar);
        _serviceRegistry.EnsureSidecarRegisteredAsync(sidecar.Id, Arg.Any<CancellationToken>()).Returns(entry);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        entry.Domains.Count.ShouldBe(1);
        entry.Domains.First().Hostname.ShouldBe("traefik.example.com");
    }
}