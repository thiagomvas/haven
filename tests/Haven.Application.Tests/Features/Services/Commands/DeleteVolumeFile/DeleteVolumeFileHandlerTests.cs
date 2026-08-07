using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Services.Commands.DeleteVolumeFile;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Commands.DeleteVolumeFile;

[Category("Unit")]
public sealed class DeleteVolumeFileHandlerTests
{
    private IServiceRepository _serviceRepository;
    private IManagedVolumeFileService _managedVolumeFileService;
    private DeleteVolumeFileHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _managedVolumeFileService = Substitute.For<IManagedVolumeFileService>();
        _sut = new DeleteVolumeFileHandler(_serviceRepository, _managedVolumeFileService);
    }

    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public async Task Handle_NonManagedVolume_ReturnsFailure()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Named, "cache", "/cache", source: "cache-vol");
        var command = new DeleteVolumeFileCommand { ServiceId = service.Id, VolumeId = volume.Id, Path = "a.txt" };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ReadOnlyVolume_ReturnsFailure_AndDoesNotDelete()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx", readOnly: true);
        var command = new DeleteVolumeFileCommand { ServiceId = service.Id, VolumeId = volume.Id, Path = "a.txt" };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _managedVolumeFileService.DidNotReceiveWithAnyArgs()
            .DeleteFileAsync(default, default, default!, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WritableManagedVolume_DeletesFile()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx");
        var command = new DeleteVolumeFileCommand { ServiceId = service.Id, VolumeId = volume.Id, Path = "a.txt" };
        _serviceRepository.GetByIdAsync(command.ServiceId, Arg.Any<CancellationToken>()).Returns(service);
        _managedVolumeFileService.DeleteFileAsync(service.Id, volume.Id, "a.txt", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _managedVolumeFileService.Received(1)
            .DeleteFileAsync(service.Id, volume.Id, "a.txt", Arg.Any<CancellationToken>());
    }
}