using System.Net;
using System.Net.Security;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TeamA.ToDo.Application.DTOs.Email;
using TeamA.ToDo.Application.Interfaces;

namespace TeamA.ToDo.Application.Email
{
    public class SmtpEmailSender : IApplicationEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<EmailSettings> emailSettings,
            ILogger<SmtpEmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            await SendEmailAsync(to, null, null, subject, htmlMessage, null);
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage, string plainTextMessage)
        {
            await SendEmailAsync(to, null, null, subject, htmlMessage, plainTextMessage);
        }

        public async Task SendEmailAsync(string to, string cc, string bcc, string subject, string htmlMessage, string plainTextMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));

                if (!string.IsNullOrEmpty(cc))
                {
                    message.Cc.Add(MailboxAddress.Parse(cc));
                }

                if (!string.IsNullOrEmpty(bcc))
                {
                    message.Bcc.Add(MailboxAddress.Parse(bcc));
                }

                message.Subject = subject;

                var builder = new BodyBuilder();

                if (!string.IsNullOrEmpty(htmlMessage))
                {
                    builder.HtmlBody = htmlMessage;
                }

                if (!string.IsNullOrEmpty(plainTextMessage))
                {
                    builder.TextBody = plainTextMessage;
                }
                else if (!string.IsNullOrEmpty(htmlMessage))
                {
                    // Generate plain text version from HTML if none provided
                    builder.TextBody = HtmlToPlainText(htmlMessage);
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();

                // Only validate certificates if SSL is enabled (avoids issues with self-signed certs in development)
                client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    !_emailSettings.Smtp.EnableSsl || errors == SslPolicyErrors.None;

                await client.ConnectAsync(
                    _emailSettings.Smtp.Host,
                    _emailSettings.Smtp.Port,
                    _emailSettings.Smtp.EnableSsl);

                // Only authenticate if credentials are provided
                if (!string.IsNullOrEmpty(_emailSettings.Smtp.Username) &&
                    !string.IsNullOrEmpty(_emailSettings.Smtp.Password))
                {
                    await client.AuthenticateAsync(_emailSettings.Smtp.Username, _emailSettings.Smtp.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Recipient}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", to);
                throw;
            }
        }

        public async Task SendEmailWithAttachmentsAsync(string to, string subject, string htmlMessage, List<EmailAttachment> attachments)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = htmlMessage;
                builder.TextBody = HtmlToPlainText(htmlMessage);

                // Add attachments
                if (attachments != null && attachments.Count > 0)
                {
                    foreach (var attachment in attachments)
                    {
                        builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                    }
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    !_emailSettings.Smtp.EnableSsl || errors == SslPolicyErrors.None;

                await client.ConnectAsync(
                    _emailSettings.Smtp.Host,
                    _emailSettings.Smtp.Port,
                    _emailSettings.Smtp.EnableSsl);

                if (!string.IsNullOrEmpty(_emailSettings.Smtp.Username) &&
                    !string.IsNullOrEmpty(_emailSettings.Smtp.Password))
                {
                    await client.AuthenticateAsync(_emailSettings.Smtp.Username, _emailSettings.Smtp.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email with attachments sent successfully to {Recipient}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with attachments to {Recipient}", to);
                throw;
            }
        }

        private string HtmlToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Simple HTML to plain text conversion
            var text = html;

            // Remove HTML tags
            text = Regex.Replace(text, "<[^>]*>", "");

            // Replace common HTML entities
            text = text.Replace("&nbsp;", " ")
                       .Replace("&amp;", "&")
                       .Replace("&lt;", "<")
                       .Replace("&gt;", ">")
                       .Replace("&quot;", "\"")
                       .Replace("&#39;", "'");

            // Decode HTML entities
            text = WebUtility.HtmlDecode(text);

            // Normalize whitespace
            text = Regex.Replace(text, @"\s+", " ");

            return text.Trim();
        }
    }
}