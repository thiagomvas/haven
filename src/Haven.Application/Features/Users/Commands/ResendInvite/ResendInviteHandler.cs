using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.SystemNotifications;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Users.Commands.ResendInvite;

public sealed class ResendInviteHandler(
    IUserRepository userRepository,
    IAuthService authService,
    INotificationChannelConfigRepository channelConfigRepository,
    ISystemNotificationEnqueuer systemNotificationEnqueuer,
    IFrontendLinkBuilder frontendLinkBuilder)
    : ICommandHandler<ResendInviteCommand>
{
    public async ValueTask<Result> Handle(ResendInviteCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Error.NotFoundFor(nameof(User), command.UserId);

        if (!user.IsPendingInvite)
            return Error.InvalidOperation("User has already completed first access.");

        var defaultSmtpConfig = await channelConfigRepository.GetSystemDefaultAsync(NotificationChannel.Smtp, cancellationToken);
        if (defaultSmtpConfig is null || !defaultSmtpConfig.Enabled)
            return Error.InvalidOperation(
                "No SMTP provider is marked as the system default. Configure one under Notification Channels before resending invites.");

        var revokeResult = await authService.RevokeInviteTokensForUserAsync(user.Id);
        if (revokeResult.IsFailure)
            return revokeResult.Error;

        var inviteToken = await authService.CreateInviteTokenAsync(user.Id);
        if (inviteToken.IsFailure)
            return inviteToken.Error;

        var inviteUrl = frontendLinkBuilder.BuildAcceptInviteUrl(inviteToken.Value.RawToken);
        var expiresInHours = (int)Math.Ceiling((inviteToken.Value.ExpiresAt - DateTime.UtcNow).TotalHours);
        var templateData = SystemNotificationTemplateData.ForFirstAccess(inviteUrl, expiresInHours);

        await systemNotificationEnqueuer.EnqueueAsync(SystemNotificationType.FirstAccess, user.Email, templateData, cancellationToken);

        return Result.Success();
    }
}