using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Sidecars.Queries.ListSidecars;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Sidecars.Queries.ListSidecars;

[Category("Unit")]
public sealed class ListSidecarsHandlerTests
{
    private ISidecarRepository _sidecarRepository;
    private ListSidecarsHandler _sut;

    [SetUp]
    public void Setup()
    {
        _sidecarRepository = Substitute.For<ISidecarRepository>();
        _sut = new ListSidecarsHandler(_sidecarRepository);
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyList_WhenNoSidecarsExist()
    {
        _sidecarRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Sidecar>() as IReadOnlyList<Sidecar>);

        var result = await _sut.Handle(new ListSidecarsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldReturnSidecarDtos_MappedCorrectly()
    {
        var sidecar = Sidecar.Create("whoami", SidecarKind.Whoami);
        _sidecarRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Sidecar> { sidecar } as IReadOnlyList<Sidecar>);

        var result = await _sut.Handle(new ListSidecarsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);

        var dto = result.Value[0];
        dto.Id.ShouldBe(sidecar.Id);
        dto.Name.ShouldBe("whoami");
        dto.Kind.ShouldBe(SidecarKind.Whoami);
        dto.Enabled.ShouldBeFalse();
        dto.Status.ShouldBe(ServiceStatus.Stopped);
    }
}