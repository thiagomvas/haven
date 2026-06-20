namespace Haven.Application.Common.Contracts.Notifications;

public class DiscordNotificationConfig
{
    public string WebhookUrl { get; set; } = string.Empty;
    public bool Embed { get; set; }
}