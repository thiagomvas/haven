using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class FeatureFlagServiceTests
{
    private FeatureFlagService _sut = null!;
    private IFeatureFlagRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _repository = Substitute.For<IFeatureFlagRepository>();
        _sut = new FeatureFlagService(_repository);
    }

    [Test]
    public async Task GetFlagsAsEnvironmentsForServiceAsync_WhenFlagsExist_ShouldMapToEnvironmentVariables()
    {
        var serviceId = Guid.NewGuid();
        var flags = new List<FeatureFlag>
        {
            FeatureFlag.Create(serviceId, "Flag1", FeatureFlagType.EnvironmentVariable, "FLAG_1", "desc", "true", FeatureFlagValueType.Bool),
            FeatureFlag.Create(serviceId, "Flag2", FeatureFlagType.EnvironmentVariable, "FLAG_2", "desc", "false", FeatureFlagValueType.Bool)
        };

        _repository.GetForServiceListAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(flags);

        var result = await _sut.GetFlagsAsEnvironmentsForServiceAsync(serviceId, CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Key.ShouldBe("FLAG_1");
        result[0].Value.ShouldBe("true");
        result[0].ParentId.ShouldBe(serviceId);
        result[0].ParentType.ShouldBe(EnvironmentVariableParentType.Service);
        result[1].Key.ShouldBe("FLAG_2");
        result[1].Value.ShouldBe("false");
        result[1].ParentId.ShouldBe(serviceId);
        result[1].ParentType.ShouldBe(EnvironmentVariableParentType.Service);
    }

    [Test]
    public async Task GetFlagsAsEnvironmentsForServiceAsync_WhenNoFlagsExist_ShouldReturnEmptyList()
    {
        var serviceId = Guid.NewGuid();

        _repository.GetForServiceListAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(new List<FeatureFlag>());

        var result = await _sut.GetFlagsAsEnvironmentsForServiceAsync(serviceId, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetFlagsAsEnvironmentsForServiceAsync_ShouldPassServiceIdAndCancellationTokenToRepository()
    {
        var serviceId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        _repository.GetForServiceListAsync(serviceId, cts.Token)
            .Returns(new List<FeatureFlag>());

        await _sut.GetFlagsAsEnvironmentsForServiceAsync(serviceId, cts.Token);

        await _repository.Received(1).GetForServiceListAsync(serviceId, cts.Token);
    }

    [Test]
    public async Task GetFlagsAsEnvironmentsForServiceAsync_WhenFlagKeyIsNull_ShouldMapNullKey()
    {
        var serviceId = Guid.NewGuid();
        var flags = new List<FeatureFlag>
        {
            FeatureFlag.Create(serviceId, "Flag1", FeatureFlagType.EnvironmentVariable, null, "desc", "true", FeatureFlagValueType.Bool)
        };

        _repository.GetForServiceListAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(flags);

        var result = await _sut.GetFlagsAsEnvironmentsForServiceAsync(serviceId, CancellationToken.None);

        result[0].Key.ShouldBeNull();
    }
}