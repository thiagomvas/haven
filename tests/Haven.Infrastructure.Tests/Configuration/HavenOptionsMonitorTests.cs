using System.Text.Json;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Infrastructure.Configuration;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Configuration;

[Category("Unit")]
public sealed class HavenOptionsMonitorTests
{
    private IServiceScopeFactory _scopeFactory = null!;
    private IServiceScope _scope = null!;
    private IServiceProvider _serviceProvider = null!;
    private IHavenSettingRepository _settingRepository = null!;
    private HavenConfigurationStore _store = null!;
    private HavenOptionsMonitor<MonitorTestSettings> _sut = null!;

    private const string SectionName = "MonitorSection";

    [SetUp]
    public void Setup()
    {
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scope = Substitute.For<IServiceScope>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _settingRepository = Substitute.For<IHavenSettingRepository>();

        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IHavenSettingRepository)).Returns(_settingRepository);

        _store = new HavenConfigurationStore(_scopeFactory);
        _sut = new HavenOptionsMonitor<MonitorTestSettings>(_store, SectionName);
    }

    [TearDown]
    public void Dispose() => _scope.Dispose();

    [Test]
    public void CurrentValue_ShouldReturnDefaultInstance_WhenRepositoryReturnsNull()
    {
        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = _sut.CurrentValue;

        result.ShouldNotBeNull();
        result.Name.ShouldBe(string.Empty);
        result.Value.ShouldBe(0);
    }

    [Test]
    public void CurrentValue_ShouldReturnDeserializedValue_WhenRepositoryReturnsJson()
    {
        var json = JsonSerializer.Serialize(new MonitorTestSettings { Name = "Test", Value = 7 });
        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>()).Returns(json);

        var result = _sut.CurrentValue;

        result.Name.ShouldBe("Test");
        result.Value.ShouldBe(7);
    }

    [Test]
    public void Get_ShouldReturnSameAsCurrentValue()
    {
        var json = JsonSerializer.Serialize(new MonitorTestSettings { Name = "Named", Value = 3 });
        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>()).Returns(json);

        var fromGet = _sut.Get("any-name");
        var fromCurrentValue = _sut.CurrentValue;

        fromGet.Name.ShouldBe(fromCurrentValue.Name);
        fromGet.Value.ShouldBe(fromCurrentValue.Value);
    }

    [Test]
    public void Get_ShouldReturnSameValue_RegardlessOfNameArgument()
    {
        var json = JsonSerializer.Serialize(new MonitorTestSettings { Name = "Consistent", Value = 5 });
        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>()).Returns(json);

        var result1 = _sut.Get(null);
        var result2 = _sut.Get("some-name");
        var result3 = _sut.Get(string.Empty);

        result1.Name.ShouldBe("Consistent");
        result2.Name.ShouldBe("Consistent");
        result3.Name.ShouldBe("Consistent");
    }

    [Test]
    public void OnChange_ShouldInvokeListener_WhenStoreIsInvalidated()
    {
        var initialJson = JsonSerializer.Serialize(new MonitorTestSettings { Name = "Before", Value = 1 });
        var updatedJson = JsonSerializer.Serialize(new MonitorTestSettings { Name = "After", Value = 2 });

        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>())
            .Returns(initialJson, updatedJson);

        MonitorTestSettings? received = null;
        _ = _sut.CurrentValue; // prime the cache
        _sut.OnChange((settings, _) => received = settings);
        _store.Invalidate(SectionName);

        received.ShouldNotBeNull();
        received!.Name.ShouldBe("After");
        received.Value.ShouldBe(2);
    }

    [Test]
    public void OnChange_ShouldNotInvokeListener_WhenDifferentSectionIsInvalidated()
    {
        var json = JsonSerializer.Serialize(new MonitorTestSettings { Name = "Stable", Value = 9 });
        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>()).Returns(json);

        var invoked = false;
        _sut.OnChange((_, _) => invoked = true);
        _store.Invalidate("OtherSection");

        invoked.ShouldBeFalse();
    }

    [Test]
    public void OnChange_ShouldNotInvokeListener_AfterRegistrationIsDisposed()
    {
        var initialJson = JsonSerializer.Serialize(new MonitorTestSettings { Name = "Initial", Value = 1 });
        var updatedJson = JsonSerializer.Serialize(new MonitorTestSettings { Name = "Updated", Value = 2 });

        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>())
            .Returns(initialJson, updatedJson);

        var invoked = false;
        _ = _sut.CurrentValue; // prime the cache
        var registration = _sut.OnChange((_, _) => invoked = true);
        registration?.Dispose();
        _store.Invalidate(SectionName);

        invoked.ShouldBeFalse();
    }

    [Test]
    public void OnChange_ShouldOnlyListenToOwnSection_WhenMultipleMonitorsExist()
    {
        const string otherSection = "OtherSection";
        var jsonA = JsonSerializer.Serialize(new MonitorTestSettings { Name = "A", Value = 1 });
        var jsonB = JsonSerializer.Serialize(new MonitorTestSettings { Name = "B", Value = 2 });

        _settingRepository.GetAsync(SectionName, Arg.Any<CancellationToken>()).Returns(jsonA);
        _settingRepository.GetAsync(otherSection, Arg.Any<CancellationToken>()).Returns(jsonB);

        var otherMonitor = new HavenOptionsMonitor<MonitorTestSettings>(_store, otherSection);

        var invokedOnSut = false;
        var invokedOnOther = false;

        _ = _sut.CurrentValue;
        _sut.OnChange((_, _) => invokedOnSut = true);

        _ = otherMonitor.CurrentValue;
        otherMonitor.OnChange((_, _) => invokedOnOther = true);

        _store.Invalidate(otherSection);

        invokedOnSut.ShouldBeFalse();
        invokedOnOther.ShouldBeTrue();
    }

    private sealed class MonitorTestSettings
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}