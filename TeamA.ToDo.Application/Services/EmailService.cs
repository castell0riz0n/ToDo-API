using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamA.ToDo.Application.DTOs.Email;
using TeamA.ToDo.Application.Email;
using TeamA.ToDo.Application.Interfaces;

namespace TeamA.ToDo.Application.Services
{
    /// <summary>
    /// Email service that selects the appropriate email provider based on configuration
    /// </summary>
    public class EmailService : IApplicationEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IServiceProvider serviceProvider,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var emailSender = GetEmailSender();
            await emailSender.SendEmailAsync(to, subject, htmlMessage);
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage, string plainTextMessage)
        {
            var emailSender = GetEmailSender();
            await emailSender.SendEmailAsync(to, subject, htmlMessage, plainTextMessage);
        }

        public async Task SendEmailAsync(string to, string cc, string bcc, string subject, string htmlMessage, string plainTextMessage)
        {
            var emailSender = GetEmailSender();
            await emailSender.SendEmailAsync(to, cc, bcc, subject, htmlMessage, plainTextMessage);
        }

        public async Task SendEmailWithAttachmentsAsync(string to, string subject, string htmlMessage, List<EmailAttachment> attachments)
        {
            var emailSender = GetEmailSender();
            await emailSender.SendEmailWithAttachmentsAsync(to, subject, htmlMessage, attachments);
        }

        /// <summary>
        /// Gets the appropriate email sender based on configuration
        /// </summary>
        private IApplicationEmailSender GetEmailSender()
        {
            string provider = _emailSettings.Provider?.ToLower() ?? "smtp";

            try
            {
                return provider switch
                {
                    "sendgrid" => (IApplicationEmailSender)_serviceProvider.GetService(typeof(SendGridEmailSender)),
                    "smtp" or _ => (IApplicationEmailSender)_serviceProvider.GetService(typeof(SmtpEmailSender))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email sender for provider {Provider}. Falling back to SMTP.", provider);
                return (IApplicationEmailSender)_serviceProvider.GetService(typeof(SmtpEmailSender));
            }
        }
    }
}