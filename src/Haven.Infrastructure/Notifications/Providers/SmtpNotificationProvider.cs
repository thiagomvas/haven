using System.Net.Mail;

using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.Notifications.Contracts;

using MailKit.Security;

using MimeKit;

namespace Haven.Infrastructure.Notifications.Providers;

public class SmtpNotificationProvider : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Smtp;
    public async Task<NotificationProviderResult> SendAsync(NotificationAttempt attempt, NotificationChannelConfig config, CancellationToken ct = default)
    {
        var smtpConfig = config.ToProviderConfig<SmtpNotificationConfig>();
        var envelope = attempt.CreateEnvelope();

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(smtpConfig.FromName, smtpConfig.FromEmail));
        email.Subject = envelope.ToFormattedEventName();
        email.Body = new BodyBuilder { TextBody = envelope.Message }.ToMessageBody();

        foreach (var toEmail in smtpConfig.ToEmails)
        {
            if (MailboxAddress.TryParse(toEmail, out var to))
            {
                email.To.Add(to);
            }
            else
            {
                return new NotificationProviderResult(false, envelope.Message, null, $"Invalid email address: {toEmail}");
            }
        }

        var secureSocketOptions = smtpConfig.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

        using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
        await smtpClient.ConnectAsync(smtpConfig.Host, smtpConfig.Port, secureSocketOptions, ct);
        await smtpClient.AuthenticateAsync(smtpConfig.Username, smtpConfig.Password, ct);
        await smtpClient.SendAsync(email, ct);
        await smtpClient.DisconnectAsync(true, ct);

        return new NotificationProviderResult(true, envelope.Message, null, null);
    }
}