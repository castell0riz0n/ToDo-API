namespace TeamA.ToDo.Application.DTOs.Email;

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// The email provider to use (smtp, sendgrid)
    /// </summary>
    public string Provider { get; set; } = "Smtp";

    /// <summary>
    /// The email address that will appear as the sender
    /// </summary>
    public string SenderEmail { get; set; }

    /// <summary>
    /// The display name that will appear as the sender
    /// </summary>
    public string SenderName { get; set; }

    /// <summary>
    /// SendGrid-specific settings
    /// </summary>
    public SendGridSettings SendGrid { get; set; }

    /// <summary>
    /// SMTP-specific settings
    /// </summary>
    public SmtpSettings Smtp { get; set; }
}