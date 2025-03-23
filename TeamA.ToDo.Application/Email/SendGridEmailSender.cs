using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using TeamA.ToDo.Application.DTOs.Email;
using TeamA.ToDo.Application.Interfaces;

namespace TeamA.ToDo.Application.Email
{
    public class SendGridEmailSender : IApplicationEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<SendGridEmailSender> _logger;

        public SendGridEmailSender(
            IOptions<EmailSettings> emailSettings,
            ILogger<SendGridEmailSender> logger)
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
                var client = new SendGridClient(_emailSettings.SendGrid.ApiKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    HtmlContent = htmlMessage,
                    PlainTextContent = plainTextMessage ?? HtmlToPlainText(htmlMessage)
                };

                msg.AddTo(new EmailAddress(to));

                if (!string.IsNullOrEmpty(cc))
                {
                    msg.AddCc(new EmailAddress(cc));
                }

                if (!string.IsNullOrEmpty(bcc))
                {
                    msg.AddBcc(new EmailAddress(bcc));
                }

                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully via SendGrid to {Recipient}", to);
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogWarning("SendGrid returned non-success status code {StatusCode}: {Response}",
                        response.StatusCode, responseBody);
                    throw new Exception($"Failed to send email via SendGrid: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SendGrid to {Recipient}", to);
                throw;
            }
        }

        public async Task SendEmailWithAttachmentsAsync(string to, string subject, string htmlMessage, List<EmailAttachment> attachments)
        {
            try
            {
                var client = new SendGridClient(_emailSettings.SendGrid.ApiKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    HtmlContent = htmlMessage,
                    PlainTextContent = HtmlToPlainText(htmlMessage)
                };

                msg.AddTo(new EmailAddress(to));

                // Add attachments
                if (attachments != null && attachments.Count > 0)
                {
                    foreach (var attachment in attachments)
                    {
                        var base64Content = Convert.ToBase64String(attachment.Content);
                        msg.AddAttachment(attachment.FileName, base64Content, attachment.ContentType);
                    }
                }

                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email with attachments sent successfully via SendGrid to {Recipient}", to);
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogWarning("SendGrid returned non-success status code {StatusCode}: {Response}",
                        response.StatusCode, responseBody);
                    throw new Exception($"Failed to send email with attachments via SendGrid: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with attachments via SendGrid to {Recipient}", to);
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