using System;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Email
{
    public interface IEmailSender
    {
        System.Threading.Tasks.Task SendAsync(EmailData emailData);
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailOptions> emailOptions, ILogger<EmailSender> logger)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task SendAsync(EmailData emailData)
        {
            _logger?.LogTrace($"Sending email to {emailData.ToAddress}. " +
                              $"Subject: {emailData.Subject}, Server: {_emailOptions.Server}");

            try
            {
                // fallback to global options if local sender is not provided
                var senderName = string.IsNullOrEmpty(emailData.FromName)
                    ? _emailOptions.SenderName
                    : emailData.FromName;
                var fromAddress = string.IsNullOrEmpty(emailData.FromAddress)
                    ? _emailOptions.FromAddress
                    : emailData.FromAddress;

                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(senderName, fromAddress));
                mimeMessage.To.Add(new MailboxAddress(emailData.ToName, emailData.ToAddress));
                mimeMessage.Subject = emailData.Subject;
                mimeMessage.Body = new TextPart("html")
                {
                    Text = emailData.Body
                };

                using var client = new SmtpClient
                {
                    // accept all SSL certificates (in case the server supports STARTTLS)
                    ServerCertificateValidationCallback = (_, _, _, _) => true
                };

                //TODO: finalize email sending security
                //if (_env.IsDevelopment())
                //{
                //    // The third parameter is useSSL (true if the client should make an SSL-wrapped
                //    // connection to the server; otherwise, false).
                //    await client.ConnectAsync(_smtpOptions.Server, _smtpOptions.Port, true);
                //}
                //else
                //{
                //    await client.ConnectAsync(_smtpOptions.Server);
                //}

                await client.ConnectAsync(_emailOptions.Server, _emailOptions.Port);

                // Note: only needed if the SMTP server requires authentication
                if (!string.IsNullOrEmpty(_emailOptions.Username))
                    await client.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password);

                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, $"Error sending email message to {emailData.ToAddress}. {ex.Message}");
            }
        }
    }
}
