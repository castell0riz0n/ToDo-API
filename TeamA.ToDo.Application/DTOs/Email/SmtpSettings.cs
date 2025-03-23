namespace TeamA.ToDo.Application.DTOs.Email;

/// <summary>
/// SMTP server configuration settings
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// SMTP server hostname
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS for the connection
    /// </summary>
    public bool EnableSsl { get; set; }

    /// <summary>
    /// SMTP username for authentication
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// SMTP password for authentication
    /// </summary>
    public string Password { get; set; }
}