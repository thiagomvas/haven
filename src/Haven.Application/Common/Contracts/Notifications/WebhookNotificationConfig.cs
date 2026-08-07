namespace Haven.Application.Common.Contracts.Notifications;

public class WebhookNotificationConfig
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
}