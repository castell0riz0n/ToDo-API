using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.DTOs.Users;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Security;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.General;

namespace TeamA.ToDo.Host.Controllers.Auth;

[ApiController]
[Route("api/user-settings")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IEmailSender _emailSender;
    private readonly IActivityLogger _activityLogger;
    private readonly ILogger<UserSettingsController> _logger;

    public UserSettingsController(
        IUserService userService,
        ITwoFactorService twoFactorService,
        IEmailSender emailSender,
        IActivityLogger activityLogger,
        ILogger<UserSettingsController> logger, UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _twoFactorService = twoFactorService;
        _emailSender = emailSender;
        _activityLogger = activityLogger;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        var response = await _userService.ChangePasswordAsync(userId, model);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        // Return 200 OK with response information
        return Ok(response);
    }

    [HttpPost("toggle-two-factor")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ToggleTwoFactor([FromBody] TwoFactorVerificationDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        // Check current 2FA status to determine whether to enable or disable
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "User not found"
            });
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        ServiceResponse<bool> response;
        if (isTwoFactorEnabled)
        {
            // If 2FA is enabled, disable it
            response = await _twoFactorService.DisableTwoFactorAuthAsync(userId, model.VerificationCode);
        }
        else
        {
            // If 2FA is disabled, enable it
            response = await _twoFactorService.EnableTwoFactorAuthAsync(userId, model.VerificationCode);
        }

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("security-log")]
    [ProducesResponseType(typeof(ServiceResponse<List<UserActivityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecurityLog([FromQuery] int take = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ServiceResponse<List<UserActivityDto>>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        // Limit the maximum records that can be requested
        if (take > 100) take = 100;

        var activities = await _activityLogger.GetUserActivityAsync(userId, take);

        // Convert to DTOs
        var activityDtos = activities.Select(a => new UserActivityDto
        {
            Action = a.Action,
            Description = a.Description,
            IpAddress = a.IpAddress,
            IsSuccessful = a.IsSuccessful,
            Timestamp = a.Timestamp
        }).ToList();

        return Ok(new ServiceResponse<List<UserActivityDto>>
        {
            Success = true,
            Data = activityDtos
        });
    }

    [HttpPost("send-verification-email")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendVerificationEmail()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "User not found"
            });
        }

        // Check if email is already confirmed
        if (user.EmailConfirmed)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "Email is already confirmed"
            });
        }

        // Generate email confirmation token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // Get app config
        var appConfig = HttpContext.RequestServices.GetRequiredService<IOptions<AppConfig>>().Value;

        // Create confirmation link
        var confirmationLink = $"{appConfig.BaseUrl}/account/confirm-email?userId={user.Id}&token={encodedToken}";

        // Get email template service
        var emailTemplateService = HttpContext.RequestServices.GetRequiredService<IEmailTemplateService>();

        // Create email content
        var emailHtml = emailTemplateService.GetEmailVerificationTemplate(
            $"{user.FirstName} {user.LastName}",
            confirmationLink,
            appConfig.AppName);

        // Send email
        await _emailSender.SendEmailAsync(
            user.Email,
            "Confirm your email",
            emailHtml);

        // Log activity
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _activityLogger.LogActivityAsync(
            user.Id,
            "Verification Email Sent",
            "User requested a new verification email",
            ipAddress,
            true);

        return Ok(new ServiceResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Verification email sent successfully"
        });
    }
}

