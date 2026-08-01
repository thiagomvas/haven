namespace Haven.Infrastructure.Notifications.Contracts;

public class SmtpNotificationConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
    public bool EnableSsl { get; set; }
    public List<string> ToEmails { get; set; } = new List<string>();
}