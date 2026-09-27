using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Application.Features.Services.ComputedOutputs;
using Haven.Application.Features.Services.Queries.GetServiceDashboard;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Queries.GetServiceDashboard;

[TestFixture]
[Category("Unit")]
public class GetServiceDashboardHandlerTests
{
    private IServiceRepository _serviceRepository = null!;
    private IEnvironmentVariableService _environmentVariableService = null!;
    private IFeatureFlagRepository _featureFlagRepository = null!;
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository = null!;
    private IComputedOutputResolver _computedOutputResolver = null!;
    private IOptionsMonitor<NetworkOptions> _networkOptions = null!;
    private GetServiceDashboardHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _serviceRepository = Substitute.For<IServiceRepository>();
        _environmentVariableService = Substitute.For<IEnvironmentVariableService>();
        _featureFlagRepository = Substitute.For<IFeatureFlagRepository>();
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _computedOutputResolver = Substitute.For<IComputedOutputResolver>();
        _networkOptions = Substitute.For<IOptionsMonitor<NetworkOptions>>();
        _networkOptions.CurrentValue.Returns(new NetworkOptions());

        _environmentVariableService.BuildVariablesForServiceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _featureFlagRepository.GetForServiceListAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FeatureFlag>)[]);

        _sut = new GetServiceDashboardHandler(
            _serviceRepository,
            _environmentVariableService,
            _featureFlagRepository,
            _serviceRegistryEntryRepository,
            _computedOutputResolver,
            _networkOptions);
    }

    private static Service NewService(Guid environmentId) =>
        Service.Create(environmentId, "svc", ServiceType.DockerImage, ExposureMode.None);

    [Test]
    public async Task Handle_ServiceWithoutComputedProperties_DoesNotCallResolver()
    {
        var environmentId = Guid.NewGuid();
        var service = NewService(environmentId);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(new GetServiceDashboardQuery { EnvironmentId = environmentId, ServiceId = service.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ComputedOutputs.ShouldBeEmpty();
        await _computedOutputResolver.DidNotReceive().ResolveAsync(Arg.Any<Service>(), Arg.Any<ServiceRegistryEntry?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ServiceWithComputedProperties_NeverExposesPlaintextSecret()
    {
        var environmentId = Guid.NewGuid();
        var service = NewService(environmentId);
        service.AddComputedProperty("connection_string", "Connection String", "postgresql://${{ env.PASSWORD }}@${{ runtime.host }}", true);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        _computedOutputResolver.ResolveAsync(service, Arg.Any<ServiceRegistryEntry?>(), Arg.Any<CancellationToken>())
            .Returns(new List<ResolvedComputedOutput>
            {
                new("connection_string", "Connection String", true, true, null,
                    Value: "postgresql://hunter2@172.18.0.5",
                    MaskedPreview: "postgresql://••••••••@172.18.0.5")
            });

        var result = await _sut.Handle(new GetServiceDashboardQuery { EnvironmentId = environmentId, ServiceId = service.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var output = result.Value.ComputedOutputs.Single();
        output.Preview.ShouldNotContain("hunter2");
        output.Preview.ShouldContain("172.18.0.5");
    }

    [Test]
    public async Task Handle_ServiceInDifferentEnvironment_ReturnsNotFound()
    {
        var service = NewService(Guid.NewGuid());
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await _sut.Handle(new GetServiceDashboardQuery { EnvironmentId = Guid.NewGuid(), ServiceId = service.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }
}
