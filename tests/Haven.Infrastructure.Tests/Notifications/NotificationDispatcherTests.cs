using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Notifications;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Notifications;

[Category("Unit")]
public sealed class NotificationDispatcherTests
{
    private INotificationAttemptRepository _attemptRepository = null!;
    private INotificationProvider _webhookProvider = null!;
    private IUnitOfWork _unitOfWork = null!;
    private ILogger<NotificationDispatcher> _logger = null!;
    private NotificationDispatcher _sut = null!;

    [SetUp]
    public void Setup()
    {
        _attemptRepository = Substitute.For<INotificationAttemptRepository>();
        _webhookProvider = Substitute.For<INotificationProvider>();
        _webhookProvider.Channel.Returns(NotificationChannel.Webhook);
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<NotificationDispatcher>>();

        _sut = new NotificationDispatcher(
            _attemptRepository,
            [_webhookProvider],
            _unitOfWork,
            _logger);
    }

    private static NotificationAttempt CreateAttempt(NotificationChannelConfig? channelConfig = null)
    {
        var attempt = NotificationAttempt.CreateEnqueued(
            Guid.NewGuid(), Guid.NewGuid(), NotificationChannel.Webhook, "project.created", "{}");

        if (channelConfig is not null)
        {
            attempt.Rule = new NotificationRule { ChannelConfig = channelConfig };
        }

        return attempt;
    }

    private static NotificationChannelConfig CreateChannelConfig() =>
        NotificationChannelConfig.Create("default", NotificationChannel.Webhook, "{}", enabled: true);

    [Test]
    public async Task DispatchAsync_ShouldDoNothing_WhenAttemptNotFound()
    {
        // Arrange
        var attemptId = Guid.NewGuid();
        _attemptRepository.GetByIdAsync(attemptId, Arg.Any<CancellationToken>())
            .Returns((NotificationAttempt?)null);

        // Act
        await _sut.DispatchAsync(attemptId);

        // Assert
        await _webhookProvider.DidNotReceive().SendAsync(
            Arg.Any<NotificationAttempt>(), Arg.Any<NotificationChannelConfig>(), Arg.Any<CancellationToken>());
        await _attemptRepository.DidNotReceive().UpdateAsync(Arg.Any<NotificationAttempt>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldMarkFailedAndSave_WhenNoProviderRegisteredForChannel()
    {
        // Arrange
        var attempt = CreateAttempt(CreateChannelConfig());
        // attempt's channel is Webhook, but only register a Discord provider
        var discordProvider = Substitute.For<INotificationProvider>();
        discordProvider.Channel.Returns(NotificationChannel.Discord);
        _sut = new NotificationDispatcher(_attemptRepository, [discordProvider], _unitOfWork, _logger);

        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Failed);
        attempt.ErrorMessage.ShouldBe("No provider registered for channel 'Webhook'.");
        await _attemptRepository.DidNotReceive().UpdateAsync(Arg.Any<NotificationAttempt>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldMarkFailedAndSave_WhenRuleIsMissing()
    {
        // Arrange
        var attempt = CreateAttempt();
        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Failed);
        attempt.ErrorMessage.ShouldBe("Channel configuration not found.");
        await _webhookProvider.DidNotReceive().SendAsync(
            Arg.Any<NotificationAttempt>(), Arg.Any<NotificationChannelConfig>(), Arg.Any<CancellationToken>());
        await _attemptRepository.DidNotReceive().UpdateAsync(Arg.Any<NotificationAttempt>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldMarkFailedAndSave_WhenChannelConfigIsMissing()
    {
        // Arrange
        var attempt = CreateAttempt();
        attempt.Rule = new NotificationRule { ChannelConfig = null };
        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Failed);
        attempt.ErrorMessage.ShouldBe("Channel configuration not found.");
        await _attemptRepository.DidNotReceive().UpdateAsync(Arg.Any<NotificationAttempt>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldMarkDelivered_WhenProviderSendSucceeds()
    {
        // Arrange
        var channelConfig = CreateChannelConfig();
        var attempt = CreateAttempt(channelConfig);
        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _webhookProvider.SendAsync(attempt, channelConfig, Arg.Any<CancellationToken>())
            .Returns(new NotificationProviderResult(true, "{\"sent\":true}", "200 OK", null));

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Delivered);
        attempt.Payload.ShouldBe("{\"sent\":true}");
        attempt.Response.ShouldBe("200 OK");
        await _attemptRepository.Received(1).UpdateAsync(attempt, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldMarkFailed_WhenProviderSendReturnsFailureWithErrorMessage()
    {
        // Arrange
        var channelConfig = CreateChannelConfig();
        var attempt = CreateAttempt(channelConfig);
        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _webhookProvider.SendAsync(attempt, channelConfig, Arg.Any<CancellationToken>())
            .Returns(new NotificationProviderResult(false, "{}", "500", "Endpoint unreachable"));

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Failed);
        attempt.ErrorMessage.ShouldBe("Endpoint unreachable");
        await _attemptRepository.Received(1).UpdateAsync(attempt, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldMarkFailedWithDefaultMessage_WhenProviderSendReturnsFailureWithoutErrorMessage()
    {
        // Arrange
        var channelConfig = CreateChannelConfig();
        var attempt = CreateAttempt(channelConfig);
        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _webhookProvider.SendAsync(attempt, channelConfig, Arg.Any<CancellationToken>())
            .Returns(new NotificationProviderResult(false, "{}", null, null));

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Failed);
        attempt.ErrorMessage.ShouldBe("Unknown error.");
    }

    [Test]
    public async Task DispatchAsync_ShouldMarkFailedAndSave_WhenProviderSendThrows()
    {
        // Arrange
        var channelConfig = CreateChannelConfig();
        var attempt = CreateAttempt(channelConfig);
        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _webhookProvider.SendAsync(attempt, channelConfig, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<NotificationProviderResult>(new InvalidOperationException("boom")));

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        attempt.Status.ShouldBe(NotificationDeliveryStatus.Failed);
        attempt.ErrorMessage.ShouldBe("boom");
        attempt.Payload.ShouldBe(string.Empty);
        attempt.Response.ShouldBeNull();
        await _attemptRepository.Received(1).UpdateAsync(attempt, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ShouldSelectMatchingProvider_WhenMultipleProvidersAreRegistered()
    {
        // Arrange
        var discordProvider = Substitute.For<INotificationProvider>();
        discordProvider.Channel.Returns(NotificationChannel.Discord);

        var channelConfig = CreateChannelConfig();
        var attempt = CreateAttempt(channelConfig);
        _sut = new NotificationDispatcher(_attemptRepository, [discordProvider, _webhookProvider], _unitOfWork, _logger);

        _attemptRepository.GetByIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(attempt);
        _webhookProvider.SendAsync(attempt, channelConfig, Arg.Any<CancellationToken>())
            .Returns(new NotificationProviderResult(true, "{}", null, null));

        // Act
        await _sut.DispatchAsync(attempt.Id);

        // Assert
        await _webhookProvider.Received(1).SendAsync(attempt, channelConfig, Arg.Any<CancellationToken>());
        await discordProvider.DidNotReceive().SendAsync(
            Arg.Any<NotificationAttempt>(), Arg.Any<NotificationChannelConfig>(), Arg.Any<CancellationToken>());
    }
}