using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.Interfaces;

namespace TeamA.ToDo.Application.Helpers;

/// <summary>
/// Adapter that implements Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
/// while using the application's IApplicationEmailSender implementation
/// </summary>
public class IdentityEmailSenderAdapter : IEmailSender
{
    private readonly IApplicationEmailSender _emailSender;
    private readonly ILogger<IdentityEmailSenderAdapter> _logger;

    public IdentityEmailSenderAdapter(
        IApplicationEmailSender emailSender,
        ILogger<IdentityEmailSenderAdapter> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <summary>
    /// Implementation of Microsoft's IEmailSender interface
    /// </summary>
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("Sending email through Identity adapter to {Email}", email);
        await _emailSender.SendEmailAsync(email, subject, htmlMessage);
    }
}