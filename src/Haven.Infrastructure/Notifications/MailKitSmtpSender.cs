using Haven.Application.Common.Contracts.Notifications;

using MailKit.Security;

using MimeKit;

namespace Haven.Infrastructure.Notifications;

/// <summary>
/// Shared MailKit send logic used by both the event-driven <see cref="Providers.SmtpNotificationProvider"/>
/// and the transactional <c>SystemNotificationSender</c> — the one piece of wire-protocol code worth
/// sharing between those two otherwise-independent pipelines.
/// </summary>
internal static class MailKitSmtpSender
{
    public static async Task SendAsync(SmtpNotificationConfig config, IEnumerable<string> toEmails, string subject,
        string textBody, string? htmlBody = null, CancellationToken ct = default)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        email.Subject = subject;
        email.Body = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody }.ToMessageBody();

        foreach (var toEmail in toEmails)
        {
            if (!MailboxAddress.TryParse(toEmail, out var to))
                throw new InvalidOperationException($"Invalid email address: {toEmail}");

            email.To.Add(to);
        }

        var secureSocketOptions = config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

        using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
        await smtpClient.ConnectAsync(config.Host, config.Port, secureSocketOptions, ct);
        await smtpClient.AuthenticateAsync(config.Username, config.Password, ct);
        await smtpClient.SendAsync(email, ct);
        await smtpClient.DisconnectAsync(true, ct);
    }
}