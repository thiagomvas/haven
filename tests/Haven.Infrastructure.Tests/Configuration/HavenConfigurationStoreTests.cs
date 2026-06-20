using System.Text.Json;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Infrastructure.Configuration;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Configuration;

[Category("Unit")]
public sealed class HavenConfigurationStoreTests
{
    private IServiceScopeFactory _scopeFactory = null!;
    private IServiceScope _scope = null!;
    private IServiceProvider _serviceProvider = null!;
    private IHavenSettingRepository _settingRepository = null!;
    private HavenConfigurationStore _sut = null!;

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

        _sut = new HavenConfigurationStore(_scopeFactory);
    }

    [TearDown]
    public void Dispose()
    {
        _scope.Dispose();
    }

    [Test]
    public void GetCurrentValue_ShouldReturnDefaultInstance_WhenRepositoryReturnsNull()
    {
        // Arrange
        var category = "TestSettings";
        _settingRepository.GetAsync(category, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act
        var result = _sut.GetCurrentValue<TestSettings>(category);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(string.Empty);
        result.Value.ShouldBe(0);
    }

    [Test]
    public void GetCurrentValue_ShouldDeserializeJson_WhenRepositoryReturnsValidJson()
    {
        // Arrange
        var category = "AppConfig";
        var json = JsonSerializer.Serialize(new TestSettings { Name = "Production", Value = 42 });

        _settingRepository.GetAsync(category, Arg.Any<CancellationToken>())
            .Returns(json);

        // Act
        var result = _sut.GetCurrentValue<TestSettings>(category);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Production");
        result.Value.ShouldBe(42);
    }

    [Test]
    public void GetCurrentValue_ShouldCacheValue_WhenCalledTwiceWithSameCategory()
    {
        // Arrange
        var category = "CachedSettings";
        var json = JsonSerializer.Serialize(new TestSettings { Name = "Cached", Value = 99 });

        _settingRepository.GetAsync(category, Arg.Any<CancellationToken>())
            .Returns(json);

        // Act
        var first = _sut.GetCurrentValue<TestSettings>(category);
        var second = _sut.GetCurrentValue<TestSettings>(category);

        // Assert
        first.ShouldBe(second);
        _settingRepository.Received(1).GetAsync(category, Arg.Any<CancellationToken>());
    }

    [Test]
    public void GetCurrentValue_ShouldReloadFromRepository_AfterInvalidate()
    {
        // Arrange
        var category = "ReloadSettings";
        var initialJson = JsonSerializer.Serialize(new TestSettings { Name = "Initial", Value = 1 });
        var updatedJson = JsonSerializer.Serialize(new TestSettings { Name = "Updated", Value = 2 });

        _settingRepository.GetAsync(category, Arg.Any<CancellationToken>())
            .Returns(initialJson, updatedJson);

        // Act
        var first = _sut.GetCurrentValue<TestSettings>(category);
        _sut.Invalidate(category);
        var second = _sut.GetCurrentValue<TestSettings>(category);

        // Assert
        first.Name.ShouldBe("Initial");
        first.Value.ShouldBe(1);
        second.Name.ShouldBe("Updated");
        second.Value.ShouldBe(2);
        _settingRepository.Received(2).GetAsync(category, Arg.Any<CancellationToken>());
    }

    [Test]
    public void Invalidate_ShouldNotAffectOtherCategories_InCache()
    {
        // Arrange
        var categoryA = "SettingsA";
        var categoryB = "SettingsB";
        var jsonA = JsonSerializer.Serialize(new TestSettings { Name = "A", Value = 1 });
        var jsonB = JsonSerializer.Serialize(new TestSettings { Name = "B", Value = 2 });

        _settingRepository.GetAsync(categoryA, Arg.Any<CancellationToken>())
            .Returns(jsonA);
        _settingRepository.GetAsync(categoryB, Arg.Any<CancellationToken>())
            .Returns(jsonB);

        // Act
        var a1 = _sut.GetCurrentValue<TestSettings>(categoryA);
        var b1 = _sut.GetCurrentValue<TestSettings>(categoryB);

        _sut.Invalidate(categoryA);

        var b2 = _sut.GetCurrentValue<TestSettings>(categoryB);

        // Assert
        _settingRepository.Received(1).GetAsync(categoryA, Arg.Any<CancellationToken>());
        _settingRepository.Received(1).GetAsync(categoryB, Arg.Any<CancellationToken>());
        b1.ShouldBe(b2);
    }

    [Test]
    public void RegisterOnChange_ShouldInvokeListener_WhenCategoryIsInvalidated()
    {
        // Arrange
        var category = "ListeningSettings";
        var initialJson = JsonSerializer.Serialize(new TestSettings { Name = "Initial", Value = 10 });
        var updatedJson = JsonSerializer.Serialize(new TestSettings { Name = "Updated", Value = 20 });

        _settingRepository.GetAsync(category, Arg.Any<CancellationToken>())
            .Returns(initialJson, updatedJson);

        var listenerInvokeCount = 0;
        void TestListener(TestSettings settings, string? option)
        {
            listenerInvokeCount++;
        }

        // Act
        _sut.GetCurrentValue<TestSettings>(category);
        _sut.RegisterOnChange<TestSettings>(category, TestListener);
        _sut.Invalidate(category);

        // Assert
        listenerInvokeCount.ShouldBe(1);
    }

    [Test]
    public void RegisterOnChange_ShouldNotInvokeListener_WhenDifferentCategoryIsInvalidated()
    {
        // Arrange
        var categoryA = "ListenersA";
        var categoryB = "ListenersB";
        var jsonA = JsonSerializer.Serialize(new TestSettings { Name = "A", Value = 1 });
        var jsonB = JsonSerializer.Serialize(new TestSettings { Name = "B", Value = 2 });

        _settingRepository.GetAsync(categoryA, Arg.Any<CancellationToken>())
            .Returns(jsonA);
        _settingRepository.GetAsync(categoryB, Arg.Any<CancellationToken>())
            .Returns(jsonB);

        var listenerInvokeCount = 0;
        void TestListener(TestSettings settings, string? option)
        {
            listenerInvokeCount++;
        }

        // Act
        _sut.GetCurrentValue<TestSettings>(categoryA);
        _sut.RegisterOnChange<TestSettings>(categoryA, TestListener);
        _sut.Invalidate(categoryB);

        // Assert
        listenerInvokeCount.ShouldBe(0);
    }

    [Test]
    public void RegisterOnChange_ShouldNotInvokeListener_AfterDisposing()
    {
        // Arrange
        var category = "DisposableSettings";
        var initialJson = JsonSerializer.Serialize(new TestSettings { Name = "Initial", Value = 5 });
        var updatedJson = JsonSerializer.Serialize(new TestSettings { Name = "Updated", Value = 10 });

        _settingRepository.GetAsync(category, Arg.Any<CancellationToken>())
            .Returns(initialJson, updatedJson);

        var listenerInvokeCount = 0;
        void TestListener(TestSettings settings, string? option)
        {
            listenerInvokeCount++;
        }

        // Act
        _sut.GetCurrentValue<TestSettings>(category);
        var registration = _sut.RegisterOnChange<TestSettings>(category, TestListener);
        registration?.Dispose();
        _sut.Invalidate(category);

        // Assert
        listenerInvokeCount.ShouldBe(0);
    }

    private sealed class TestSettings
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}