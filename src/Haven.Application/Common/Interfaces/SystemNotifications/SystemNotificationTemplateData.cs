namespace Haven.Application.Common.Interfaces.SystemNotifications;

/// <summary>
/// Typed builders for the placeholder dictionaries passed through <see cref="ISystemNotificationEnqueuer"/>/
/// <see cref="ISystemNotificationSender"/>. The dictionary shape keeps the transport generic (and
/// trivially serializable as a Hangfire job argument); these builders keep call sites typed.
/// </summary>
public static class SystemNotificationTemplateData
{
    public static IReadOnlyDictionary<string, string> ForFirstAccess(string inviteUrl, int expiresInHours) =>
        new Dictionary<string, string>
        {
            ["inviteUrl"] = inviteUrl,
            ["expiresInHours"] = expiresInHours.ToString()
        };
}