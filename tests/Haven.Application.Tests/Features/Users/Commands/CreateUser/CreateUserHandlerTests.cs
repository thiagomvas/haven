using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.SystemNotifications;
using Haven.Application.Features.Users.Commands.CreateUser;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Users.Commands.CreateUser;

[Category("Unit")]
public sealed class CreateUserHandlerTests
{
    private IUserRepository _userRepository;
    private IAuthService _authService;
    private INotificationChannelConfigRepository _channelConfigRepository;
    private ISystemNotificationEnqueuer _systemNotificationEnqueuer;
    private IFrontendLinkBuilder _frontendLinkBuilder;
    private CreateUserHandler _sut;

    [SetUp]
    public void Setup()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _authService = Substitute.For<IAuthService>();
        _channelConfigRepository = Substitute.For<INotificationChannelConfigRepository>();
        _systemNotificationEnqueuer = Substitute.For<ISystemNotificationEnqueuer>();
        _frontendLinkBuilder = Substitute.For<IFrontendLinkBuilder>();
        _sut = new CreateUserHandler(
            _userRepository, _authService, _channelConfigRepository, _systemNotificationEnqueuer, _frontendLinkBuilder);
    }

    private static CreateUserCommand CreateCommand() => new()
    {
        Email = "invitee@example.com",
        IsAdmin = false,
        Permissions = []
    };

    private NotificationChannelConfig CreateEnabledSmtpConfig() =>
        NotificationChannelConfig.Create("Primary SMTP", NotificationChannel.Smtp, "{}", enabled: true);

    [Test]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflict_AndNotCreateUser()
    {
        var command = CreateCommand();
        _userRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(Error.Conflict.Code);
        await _authService.DidNotReceive().CreateUserAsync(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Handle_WhenNoSystemDefaultSmtpConfigured_ShouldReturnError_AndNotCreateUser()
    {
        var command = CreateCommand();
        _userRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _channelConfigRepository.GetSystemDefaultAsync(NotificationChannel.Smtp, Arg.Any<CancellationToken>())
            .Returns((NotificationChannelConfig?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _authService.DidNotReceive().CreateUserAsync(Arg.Any<string>(), Arg.Any<bool>());
        await _systemNotificationEnqueuer.DidNotReceive().EnqueueAsync(
            Arg.Any<SystemNotificationType>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSystemDefaultSmtpDisabled_ShouldReturnError_AndNotCreateUser()
    {
        var command = CreateCommand();
        _userRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        var disabledConfig = NotificationChannelConfig.Create("Old SMTP", NotificationChannel.Smtp, "{}", enabled: false);
        _channelConfigRepository.GetSystemDefaultAsync(NotificationChannel.Smtp, Arg.Any<CancellationToken>())
            .Returns(disabledConfig);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _authService.DidNotReceive().CreateUserAsync(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Test]
    public async Task Handle_WhenSmtpConfigured_ShouldCreateUser_AndEnqueueInviteEmail()
    {
        var command = CreateCommand();
        var user = User.CreatePending(command.Email, command.IsAdmin);

        _userRepository.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _channelConfigRepository.GetSystemDefaultAsync(NotificationChannel.Smtp, Arg.Any<CancellationToken>())
            .Returns(CreateEnabledSmtpConfig());
        _authService.CreateUserAsync(command.Email, command.IsAdmin).Returns(Result<Guid>.Success(user.Id));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _authService.CreateInviteTokenAsync(user.Id)
            .Returns(Result<InviteTokenResult>.Success(new InviteTokenResult("raw-token", DateTime.UtcNow.AddHours(72))));
        _frontendLinkBuilder.BuildAcceptInviteUrl("raw-token").Returns("https://haven.local/accept-invite?token=raw-token");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe(command.Email);
        await _systemNotificationEnqueuer.Received(1).EnqueueAsync(
            SystemNotificationType.FirstAccess,
            command.Email,
            Arg.Is<IReadOnlyDictionary<string, string>>(d => d["inviteUrl"] == "https://haven.local/accept-invite?token=raw-token"),
            Arg.Any<CancellationToken>());
    }
}
