using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Services.ComputedOutputs;
using Haven.Application.Features.Services.Queries.GetServiceComputedOutputValue;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Queries.GetServiceComputedOutputValue;

[TestFixture]
[Category("Unit")]
public class GetServiceComputedOutputValueHandlerTests
{
    private IServiceRepository _serviceRepository = null!;
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository = null!;
    private IComputedOutputResolver _computedOutputResolver = null!;
    private GetServiceComputedOutputValueHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _computedOutputResolver = Substitute.For<IComputedOutputResolver>();
        _sut = new GetServiceComputedOutputValueHandler(_serviceRepository, _serviceRegistryEntryRepository, _computedOutputResolver);
    }

    private static Service NewService(Guid environmentId) =>
        Service.Create(environmentId, "svc", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public async Task Handle_AvailableOutput_ReturnsPlaintextValue()
    {
        var environmentId = Guid.NewGuid();
        var service = NewService(environmentId);
        service.AddComputedProperty("connection_string", "Connection String", "template", true);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _computedOutputResolver.ResolveAsync(service, Arg.Any<ServiceRegistryEntry?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ResolvedComputedOutput>
            {
                new("connection_string", "Connection String", true, true, null, "postgresql://user:pass@172.18.0.5:5432/db", "masked")
            });

        var result = await _sut.Handle(new GetServiceComputedOutputValueQuery
        {
            EnvironmentId = environmentId,
            ServiceId = service.Id,
            Key = "connection_string"
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("postgresql://user:pass@172.18.0.5:5432/db");
    }

    [Test]
    public async Task Handle_UnavailableOutput_ReturnsFailure()
    {
        var environmentId = Guid.NewGuid();
        var service = NewService(environmentId);
        service.AddComputedProperty("connection_string", "Connection String", "template", true);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _computedOutputResolver.ResolveAsync(service, Arg.Any<ServiceRegistryEntry?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ResolvedComputedOutput>
            {
                new("connection_string", "Connection String", true, false, "NotRunning", null, null)
            });

        var result = await _sut.Handle(new GetServiceComputedOutputValueQuery
        {
            EnvironmentId = environmentId,
            ServiceId = service.Id,
            Key = "connection_string"
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_UnknownKey_ReturnsNotFound()
    {
        var environmentId = Guid.NewGuid();
        var service = NewService(environmentId);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(new GetServiceComputedOutputValueQuery
        {
            EnvironmentId = environmentId,
            ServiceId = service.Id,
            Key = "does_not_exist"
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ServiceInDifferentEnvironment_ReturnsNotFound()
    {
        var service = NewService(Guid.NewGuid());
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(new GetServiceComputedOutputValueQuery
        {
            EnvironmentId = Guid.NewGuid(),
            ServiceId = service.Id,
            Key = "connection_string"
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }
}