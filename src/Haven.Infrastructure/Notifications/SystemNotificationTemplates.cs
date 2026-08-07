using Haven.Domain;

namespace Haven.Infrastructure.Notifications;

/// <summary>
/// Hardcoded subject/body templates for system (transactional) emails, keyed by
/// <see cref="SystemNotificationType"/>. No DB-backed template editor for now — adding a new
/// notification type (e.g. password recovery) just means adding a new switch arm here.
/// </summary>
internal static class SystemNotificationTemplates
{
    public static (string Subject, string Body) Render(SystemNotificationType type, IReadOnlyDictionary<string, string> data) =>
        type switch
        {
            SystemNotificationType.FirstAccess => (
                "Welcome to Haven — set up your account",
                $"""
                 You've been invited to Haven. Click the link below to set your name and password:

                 {data["inviteUrl"]}

                 This link expires in {data["expiresInHours"]} hours.
                 """
            ),
            _ => throw new NotSupportedException($"No template registered for {type}.")
        };
}
