using System.Net;

using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Infrastructure.Notifications;

/// <summary>
/// Hardcoded subject/body templates for system (transactional) emails, keyed by
/// <see cref="SystemNotificationType"/>. No DB-backed template editor for now — adding a new
/// notification type (e.g. password recovery) just means adding a new switch arm here. Each
/// template renders both an HTML body (for clients that support it) and a plain-text fallback.
/// </summary>
internal static class SystemNotificationTemplates
{
    private const string BrandColor = "#1d9e75";
    private const string BrandColorHover = "#0f6e56";
    private const string TextPrimary = "#1a1f1e";
    private const string TextMuted = "#858a88";
    private const string PageBackground = "#f5f5f3";
    private const string BorderColor = "#dddbd8";

    private const string LogoUrl = "https://raw.githubusercontent.com/thiagomvas/haven/master/assets/email-logo.png";

    public static (string Subject, string TextBody, string HtmlBody) Render(
        SystemNotificationType type, IReadOnlyDictionary<string, string> data) =>
        type switch
        {
            SystemNotificationType.FirstAccess => RenderFirstAccess(data),
            _ => throw new NotSupportedException($"No template registered for {type}.")
        };

    private static (string, string, string) RenderFirstAccess(IReadOnlyDictionary<string, string> data)
    {
        var inviteUrl = data["inviteUrl"];
        var expiresInHours = data["expiresInHours"];

        const string subject = "Welcome to Haven - Set up your account";

        var text = $"""
                    You've been invited to Haven.

                    Set up your account by visiting the link below to choose your name and password:
                    {inviteUrl}

                    This link expires in {expiresInHours} hours. If you weren't expecting this invite, you can ignore this email.
                    """;

        var bodyHtml = $"""
                        <tr>
                          <td style="padding: 8px 40px 0;">
                            <h1 style="margin: 0 0 16px; font-family: 'Poppins', Helvetica, Arial, sans-serif; font-size: 22px; line-height: 1.3; color: {TextPrimary};">
                              You're invited to Haven
                            </h1>
                            <p style="margin: 0 0 24px; font-size: 15px; line-height: 1.6; color: {TextPrimary};">
                              An admin has created an account for you. Click the button below to choose your name and set a password — this activates your account and signs you in.
                            </p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding: 0 40px 28px;" align="center">
                            <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                              <tr>
                                <td style="border-radius: 8px; background-color: {BrandColor};">
                                  <a href="{HtmlEncode(inviteUrl)}" target="_blank"
                                     style="display: inline-block; padding: 12px 28px; font-family: Helvetica, Arial, sans-serif; font-size: 15px; font-weight: 600; color: #ffffff; text-decoration: none; border-radius: 8px;">
                                    Set up your account
                                  </a>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding: 0 40px 28px;">
                            <p style="margin: 0; font-size: 13px; line-height: 1.6; color: {TextMuted};">
                              This link expires in {HtmlEncode(expiresInHours)} hours. If the button above doesn't work, copy and paste this URL into your browser:
                            </p>
                            <p style="margin: 8px 0 0; font-size: 13px; line-height: 1.6; word-break: break-all;">
                              <a href="{HtmlEncode(inviteUrl)}" target="_blank" style="color: {BrandColorHover};">{HtmlEncode(inviteUrl)}</a>
                            </p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding: 0 40px 32px;">
                            <p style="margin: 0; font-size: 13px; line-height: 1.6; color: {TextMuted};">
                              If you weren't expecting an invite to Haven, you can safely ignore this email.
                            </p>
                          </td>
                        </tr>
                        """;

        return (subject, text, Wrap("You're invited to Haven", bodyHtml));
    }

    /// <summary>Shared header/footer chrome around a template's content rows.</summary>
    private static string Wrap(string previewText, string contentRows) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>Haven</title>
        </head>
        <body style="margin: 0; padding: 0; background-color: {PageBackground}; font-family: Helvetica, Arial, sans-serif;">
          <div style="display: none; max-height: 0; overflow: hidden; opacity: 0;">{HtmlEncode(previewText)}</div>
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color: {PageBackground};">
            <tr>
              <td align="center" style="padding: 40px 16px;">
                <table role="presentation" width="480" cellpadding="0" cellspacing="0" border="0"
                       style="width: 480px; max-width: 100%; background-color: #ffffff; border: 1px solid {BorderColor}; border-radius: 12px; overflow: hidden;">
                  <tr>
                    <td style="padding: 32px 40px 24px;" align="center">
                      <img src="{LogoUrl}" width="44" height="44" alt="Haven"
                           style="display: block; width: 44px; height: 44px; margin: 0 auto; border: 0; outline: none;" />
                      <div style="margin-top: 12px; font-family: 'Poppins', Helvetica, Arial, sans-serif; font-weight: 700; font-size: 18px; color: {TextPrimary};">
                        Haven
                      </div>
                    </td>
                  </tr>
                  {contentRows}
                  <tr>
                    <td style="padding: 20px 40px 32px; border-top: 1px solid {BorderColor};">
                      <p style="margin: 16px 0 0; font-size: 12px; line-height: 1.6; color: {TextMuted}; text-align: center;">
                        This is an automated message from your Haven instance.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
}
