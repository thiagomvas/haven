namespace Haven.Infrastructure.Notifications.Contracts;

public class NtfyNotificationConfig
{
    public string Host { get; set; } = $"ntfy.sh";
    public string Queue { get; set; } = string.Empty;
    public bool EnableSSL { get; set; } = false;

    public string ToUrl()
    {
        var scheme = EnableSSL ? "https" : "http";
        return $"{scheme}://{Host}/{Queue}";
    }
}