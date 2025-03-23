using TeamA.ToDo.Application.DTOs.Email;

namespace TeamA.ToDo.Application.Interfaces;

/// <summary>
/// Represents the application's extended email sending capabilities
/// </summary>
public interface IApplicationEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlMessage);
    Task SendEmailAsync(string to, string subject, string htmlMessage, string plainTextMessage);
    Task SendEmailAsync(string to, string cc, string bcc, string subject, string htmlMessage, string plainTextMessage);
    Task SendEmailWithAttachmentsAsync(string to, string subject, string htmlMessage, List<EmailAttachment> attachments);
}