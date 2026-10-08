using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Email;

namespace SenseNet.Authentication.Local;

/// <summary>Replace in a custom host to integrate a transactional mail service.</summary>
public interface ILocalPasswordResetSender
{
    Task SendAsync(EmailData message, CancellationToken cancellationToken);
}

/// <summary>Uses repository SMTP settings without the legacy sender's error suppression.</summary>
internal sealed class LocalPasswordResetSender(IOptions<EmailOptions> email, LocalAuthenticationOptions local)
    : ILocalPasswordResetSender
{
    public async Task SendAsync(EmailData message, CancellationToken cancellationToken)
    {
        var settings = email.Value;
        if (string.IsNullOrWhiteSpace(settings.Server) || settings.Port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("Repository SMTP is not configured.");
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.SenderName ?? "sensenet", settings.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName ?? "", message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("html") { Text = message.Body };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var client = new SmtpClient { Timeout = 20000 };
        // Certificate validation stays enabled. Plain SMTP is an explicit test-only opt-in.
        var security = settings.Port == 465 ? SecureSocketOptions.SslOnConnect :
            local.PasswordRecovery.RequireSmtpTls ? SecureSocketOptions.StartTls : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(settings.Server, settings.Port, security, timeout.Token).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(settings.Username))
            await client.AuthenticateAsync(settings.Username, settings.Password, timeout.Token).ConfigureAwait(false);
        await client.SendAsync(mime, timeout.Token).ConfigureAwait(false);
        await client.DisconnectAsync(true, timeout.Token).ConfigureAwait(false);
    }
}
