using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.SystemNotifications;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserHandler(
    IUserRepository userRepository,
    IAuthService authService,
    INotificationChannelConfigRepository channelConfigRepository,
    ISystemNotificationEnqueuer systemNotificationEnqueuer,
    IFrontendLinkBuilder frontendLinkBuilder)
    : ICommandHandler<CreateUserCommand, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Error.ConflictFor(nameof(User), command.Email);

        var defaultSmtpConfig = await channelConfigRepository.GetSystemDefaultAsync(NotificationChannel.Smtp, cancellationToken);
        if (defaultSmtpConfig is null || !defaultSmtpConfig.Enabled)
            return Error.InvalidOperation(
                "No SMTP provider is marked as the system default. Configure one under Notification Channels before inviting users.");

        var result = await authService.CreateUserAsync(command.Email, command.IsAdmin);
        if (result.IsFailure)
            return result.Error;

        var user = await userRepository.GetByIdAsync(result.Value, cancellationToken);
        if (user is null)
            return Error.NotFoundFor(nameof(User), result.Value);

        if (!command.IsAdmin && command.Permissions.Length > 0)
            user.SetPermissions(command.Permissions);

        var inviteToken = await authService.CreateInviteTokenAsync(user.Id);
        if (inviteToken.IsFailure)
            return inviteToken.Error;

        await SendInviteEmailAsync(user.Email, inviteToken.Value, cancellationToken);

        return Result<UserDto>.CreatedFor(new UserDto(user.Id, user.Name, user.Email, user.IsAdmin, user.RequirePasswordChange));
    }

    private async Task SendInviteEmailAsync(string email, InviteTokenResult inviteToken, CancellationToken cancellationToken)
    {
        var inviteUrl = frontendLinkBuilder.BuildAcceptInviteUrl(inviteToken.RawToken);
        var expiresInHours = (int)Math.Ceiling((inviteToken.ExpiresAt - DateTime.UtcNow).TotalHours);
        var templateData = SystemNotificationTemplateData.ForFirstAccess(inviteUrl, expiresInHours);

        await systemNotificationEnqueuer.EnqueueAsync(SystemNotificationType.FirstAccess, email, templateData, cancellationToken);
    }
}
