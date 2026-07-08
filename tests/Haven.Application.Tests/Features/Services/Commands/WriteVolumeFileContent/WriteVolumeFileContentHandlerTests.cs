using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Services.Commands.WriteVolumeFileContent;
using Haven.Domain;
using Haven.Domain.Entities;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Commands.WriteVolumeFileContent;

[Category("Unit")]
public sealed class WriteVolumeFileContentHandlerTests
{
    private IServiceRepository _serviceRepository;
    private IManagedVolumeFileService _managedVolumeFileService;
    private WriteVolumeFileContentHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _managedVolumeFileService = Substitute.For<IManagedVolumeFileService>();
        _sut = new WriteVolumeFileContentHandler(_serviceRepository, _managedVolumeFileService);
    }

    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public async Task Handle_ServiceDoesNotExist_ReturnsFailure()
    {
        var command = new WriteVolumeFileContentCommand { ServiceId = Guid.NewGuid(), VolumeId = Guid.NewGuid(), Path = "a.txt", Content = "x" };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_NonManagedVolume_ReturnsFailure()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Named, "cache", "/cache", source: "cache-vol");
        var command = new WriteVolumeFileContentCommand { ServiceId = service.Id, VolumeId = volume.Id, Path = "a.txt", Content = "x" };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _managedVolumeFileService.DidNotReceiveWithAnyArgs()
            .WriteFileAsync(default, default, default!, default!, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ReadOnlyVolume_ReturnsFailure_AndDoesNotWrite()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx", readOnly: true);
        var command = new WriteVolumeFileContentCommand { ServiceId = service.Id, VolumeId = volume.Id, Path = "a.txt", Content = "x" };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _managedVolumeFileService.DidNotReceiveWithAnyArgs()
            .WriteFileAsync(default, default, default!, default!, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WritableManagedVolume_WritesFile()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx");
        var command = new WriteVolumeFileContentCommand { ServiceId = service.Id, VolumeId = volume.Id, Path = "a.txt", Content = "x" };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);
        _managedVolumeFileService.WriteFileAsync(service.Id, volume.Id, "a.txt", "x", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _managedVolumeFileService.Received(1)
            .WriteFileAsync(service.Id, volume.Id, "a.txt", "x", Arg.Any<CancellationToken>());
    }
}
