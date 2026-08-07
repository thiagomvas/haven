using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Services.Queries.GetVolumeFileContent;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Queries.GetVolumeFileContent;

[Category("Unit")]
public sealed class GetVolumeFileContentHandlerTests
{
    private IServiceRepository _serviceRepository;
    private IManagedVolumeFileService _managedVolumeFileService;
    private GetVolumeFileContentHandler _sut;

    [SetUp]
    public void Setup()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _managedVolumeFileService = Substitute.For<IManagedVolumeFileService>();
        _sut = new GetVolumeFileContentHandler(_serviceRepository, _managedVolumeFileService);
    }

    private static Service NewService() =>
        Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public async Task Handle_ReadOnlyManagedVolume_StillAllowsRead()
    {
        var service = NewService();
        var volume = service.AddVolume(VolumeType.Managed, "config", "/etc/nginx", readOnly: true);
        var query = new GetVolumeFileContentQuery { ServiceId = service.Id, VolumeId = volume.Id, Path = "a.txt" };
        _serviceRepository.GetByIdAsync(query.ServiceId, Arg.Any<CancellationToken>()).Returns(service);
        _managedVolumeFileService.ReadFileAsync(service.Id, volume.Id, "a.txt", Arg.Any<CancellationToken>())
            .Returns("hello");

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }
}