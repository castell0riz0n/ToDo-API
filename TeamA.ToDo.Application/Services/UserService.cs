using System.Net;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using SendGrid.Helpers.Mail;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.DTOs.Users;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Security;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.Core.Models.General;
using TeamA.ToDo.EntityFramework;
using IEmailSender = Microsoft.AspNetCore.Identity.UI.Services.IEmailSender;

namespace TeamA.ToDo.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IActivityLogger _activityLogger;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AppConfig> _appConfig;
    private readonly ApplicationDbContext _context;

    public UserService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ITokenService tokenService,
        IMapper mapper,
        ILogger<UserService> logger, IOptions<AppConfig> appConfig, IEmailTemplateService emailTemplateService, IActivityLogger activityLogger, IHttpContextAccessor httpContextAccessor, RoleManager<ApplicationRole> roleManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _tokenService = tokenService;
        _mapper = mapper;
        _logger = logger;
        _appConfig = appConfig;
        _emailTemplateService = emailTemplateService;
        _activityLogger = activityLogger;
        _httpContextAccessor = httpContextAccessor;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<ServiceResponse<PagedResponse<UserProfileDto>>> GetAllUsersAsync(PaginationParams paginationParams)
    {
        var response = new ServiceResponse<PagedResponse<UserProfileDto>>();

        try
        {
            // Create base query
            var query = _userManager.Users.AsQueryable();

            // Apply search if provided
            if (!string.IsNullOrEmpty(paginationParams.SearchTerm))
            {
                var searchTerm = paginationParams.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm)
                );
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = ApplySorting(query, paginationParams);

            // Apply pagination
            var users = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            // Map to DTOs
            var userDtos = new List<UserProfileDto>();
            foreach (var user in users)
            {
                var userDto = _mapper.Map<UserProfileDto>(user);
                userDto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
                userDtos.Add(userDto);
            }

            // Create paged response
            var pagedResponse = new PagedResponse<UserProfileDto>
            {
                Items = userDtos,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize)
            };

            response.Data = pagedResponse;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            response.Success = false;
            response.Message = "Failed to retrieve users";
            return response;
        }
    }

    private IQueryable<ApplicationUser> ApplySorting(IQueryable<ApplicationUser> query, PaginationParams paginationParams)
    {
        // Default sort by CreatedAt descending if not specified
        if (string.IsNullOrEmpty(paginationParams.SortBy))
        {
            return query.OrderByDescending(u => u.CreatedAt);
        }

        // Apply specified sorting
        switch (paginationParams.SortBy.ToLower())
        {
            case "email":
                return paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email);

            case "firstname":
                return paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.FirstName)
                    : query.OrderBy(u => u.FirstName);

            case "lastname":
                return paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.LastName)
                    : query.OrderBy(u => u.LastName);

            case "lastlogin":
                return paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.LastLoginAt)
                    : query.OrderBy(u => u.LastLoginAt);

            case "isactive":
                return paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.IsActive)
                    : query.OrderBy(u => u.IsActive);

            case "createdat":
            default:
                return paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt);
        }
    }

    public async Task<ServiceResponse<string>> RegisterUserAsync(UserRegistrationDto model)
    {
        var response = new ServiceResponse<string>();

        try
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                response.Success = false;
                response.Message = "Email is already registered";
                return response;
            }

            // Map the DTO to user entity
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                DateOfBirth = model.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = false
            };

            // Create the user
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "User registration failed";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Add user to the User role
            await _userManager.AddToRoleAsync(user, "User");

            // Create default budget alert settings
            var budgetAlertSettings = new BudgetAlertSetting
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EnableAlerts = true,
                ThresholdPercentage = 80, // Alert when 80% of budget is used
                SendMonthlySummary = true
            };

            await _context.BudgetAlertSettings.AddAsync(budgetAlertSettings);
            await _context.SaveChangesAsync();

            // Generate email confirmation token
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));

            // Create confirmation link
            var confirmationLink = $"{_appConfig.Value.BaseUrl}/account/confirm-email?userId={user.Id}&token={encodedToken}";

            // Create email content using template
            var emailHtml = _emailTemplateService.GetEmailVerificationTemplate(
                $"{user.FirstName} {user.LastName}",
            confirmationLink,
                _appConfig.Value.AppName);

            // Send confirmation email
            await _emailSender.SendEmailAsync(
                user.Email,
                "Confirm your email",
                emailHtml);

            // Log the activity
            await _activityLogger.LogActivityAsync(
                user.Id,
                "Registration",
                $"User registered with email {user.Email}",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                true);

            _logger.LogInformation($"User {user.Email} registered successfully");
            response.Data = user.Id;
            response.Message = "Registration successful. Please check your email to confirm your account.";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            response.Success = false;
            response.Message = "Registration failed due to server error";
            return response;
        }
    }

    public async Task<ServiceResponse<AuthResponseDto>> LoginAsync(LoginDto model)
    {
        var response = new ServiceResponse<AuthResponseDto>();

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "Invalid email or password";
                return response;
            }

            if (!user.IsActive)
            {
                response.Success = false;
                response.Message = "Account is disabled. Please contact support.";
                return response;
            }

            // Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                response.Success = false;
                response.Message = "Email not confirmed. Please check your email for confirmation link.";
                return response;
            }

            // Get IP address for tracking
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Verify password with lockout tracking
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);

            // Check if the account just got locked out
            if (result.IsLockedOut)
            {
                // Calculate lockout duration
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var currentTime = DateTimeOffset.UtcNow;
                var lockoutMinutes = (int)Math.Ceiling((lockoutEnd - currentTime)?.TotalMinutes ?? 15);

                // Send lockout notification email
                await SendAccountLockedNotificationAsync(user, lockoutMinutes);

                response.Success = false;
                response.Message = $"Account temporarily locked due to multiple failed login attempts. Try again in {lockoutMinutes} minutes.";
                return response;
            }

            if (!result.Succeeded)
            {
                // Log the failed attempt
                await _activityLogger.LogActivityAsync(
                    user.Id,
                    "Failed Login",
                    $"Failed login attempt from IP: {ipAddress}",
                    ipAddress,
                    false);

                response.Success = false;
                response.Message = "Invalid email or password";
                return response;
            }

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Save refresh token to database
            var newRefreshToken = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);

            // Log the successful login
            await _activityLogger.LogActivityAsync(
                user.Id,
                "Login",
                $"Successful login from IP: {ipAddress}",
                ipAddress,
                true);

            // Create response
            response.Data = new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresIn = 3600, // 1 hour in seconds
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            };

            _logger.LogInformation($"User {user.Email} logged in successfully");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            response.Success = false;
            response.Message = "Login failed due to server error";
            return response;
        }
    }

    public async Task<ServiceResponse<string>> ForgotPasswordAsync(string email)
    {
        var response = new ServiceResponse<string>();

        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // For security reasons, don't reveal that the user doesn't exist
                response.Message = "If your email is registered, you will receive a password reset link";
                return response;
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Create reset link
            var resetLink = $"{_appConfig.Value.BaseUrl}/account/reset-password?email={WebUtility.UrlEncode(user.Email)}&token={encodedToken}";

            // Create email content using template
            var emailHtml = _emailTemplateService.GetPasswordResetTemplate(
                $"{user.FirstName} {user.LastName}",
                resetLink,
                _appConfig.Value.AppName);

            // Send email
            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset your password",
                emailHtml);

            // Log the activity
            await _activityLogger.LogActivityAsync(
                user.Id,
                "Password Reset Request",
                "User requested password reset",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                true);

            _logger.LogInformation($"Password reset email sent to {user.Email}");
            response.Message = "If your email is registered, you will receive a password reset link";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password");
            // For security, use the same message
            response.Message = "If your email is registered, you will receive a password reset link";
            return response;
        }
    }

    public async Task<ServiceResponse<string>> ResetPasswordAsync(ResetPasswordDto model)
    {
        var response = new ServiceResponse<string>();

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                response.Success = false;
                response.Message = "Password reset failed";
                return response;
            }

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));

            // Reset password
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Password reset failed";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Revoke all refresh tokens for security
            foreach (var refreshToken in user.RefreshTokens)
            {
                refreshToken.IsRevoked = true;
            }
            await _userManager.UpdateAsync(user);

            _logger.LogInformation($"Password reset successful for {user.Email}");
            response.Message = "Password has been reset successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            response.Success = false;
            response.Message = "Password reset failed due to server error";
            return response;
        }
    }

    public async Task<ServiceResponse<UserProfileDto>> GetUserProfileAsync(string userId)
    {
        var response = new ServiceResponse<UserProfileDto>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            var userProfile = _mapper.Map<UserProfileDto>(user);
            response.Data = userProfile;

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            response.Success = false;
            response.Message = "Failed to retrieve user profile";
            return response;
        }
    }

    public async Task<ServiceResponse<UserProfileDto>> UpdateUserProfileAsync(string userId, UserUpdateDto model)
    {
        var response = new ServiceResponse<UserProfileDto>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Update user properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DateOfBirth = model.DateOfBirth;

            // Update email if changed
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                // Check if the new email is already registered
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    response.Success = false;
                    response.Message = "Email is already registered by another user";
                    return response;
                }

                // Generate email change token
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                var result = await _userManager.ChangeEmailAsync(user, model.Email, token);

                if (!result.Succeeded)
                {
                    response.Success = false;
                    response.Message = "Failed to update email";
                    response.Errors = result.Errors.Select(e => e.Description).ToList();
                    return response;
                }

                // Update username to match the new email
                await _userManager.SetUserNameAsync(user, model.Email);
            }

            // Save changes
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to update profile";
                response.Errors = updateResult.Errors.Select(e => e.Description).ToList();
                return response;
            }

            _logger.LogInformation($"Profile updated for user {user.Email}");
            response.Data = _mapper.Map<UserProfileDto>(user);
            response.Message = "Profile updated successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            response.Success = false;
            response.Message = "Failed to update profile due to server error";
            return response;
        }
    }

    public async Task<ServiceResponse<UserProfileDto>> AdminUpdateUserAsync(string userId, AdminUserUpdateDto model)
    {
        var response = new ServiceResponse<UserProfileDto>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Map update DTO properties to user
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DateOfBirth = model.DateOfBirth;
            user.IsActive = model.IsActive;

            // Handle email change if needed
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                // Check if the new email is already registered
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    response.Success = false;
                    response.Message = "Email is already registered by another user";
                    return response;
                }

                // Update email and username
                user.Email = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.UserName = model.Email;
                user.NormalizedUserName = model.Email.ToUpper();
            }

            // Save user changes
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to update user";
                response.Errors = updateResult.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Update user roles if provided
            if (model.RoleIds != null && model.RoleIds.Any())
            {
                // Get current user roles
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove all current roles
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Get role names from role IDs
                var roles = new List<string>();
                foreach (var roleId in model.RoleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    if (role != null)
                    {
                        roles.Add(role.Name);
                    }
                }

                // Add new roles
                if (roles.Any())
                {
                    var addToRolesResult = await _userManager.AddToRolesAsync(user, roles);
                    if (!addToRolesResult.Succeeded)
                    {
                        _logger.LogWarning("Failed to update user roles: {Errors}",
                            string.Join(", ", addToRolesResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            // Get updated roles for response
            var updatedRoles = await _userManager.GetRolesAsync(user);

            // Map to response DTO
            var userDto = _mapper.Map<UserProfileDto>(user);
            userDto.Roles = updatedRoles.ToList();

            // Log activity
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var adminId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _activityLogger.LogActivityAsync(
                userId,
                "User Updated By Admin",
                $"User profile updated by admin (ID: {adminId})",
                ipAddress,
                true);

            _logger.LogInformation("Admin (ID: {AdminId}) updated user {UserId}", adminId, userId);
            response.Data = userDto;
            response.Message = "User updated successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            response.Success = false;
            response.Message = "Failed to update user due to server error";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> ConfirmEmailAsync(string userId, string token)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            // Confirm email
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Email confirmation failed";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Get client application URL for login
            var loginLink = $"{_appConfig.Value.BaseUrl}/login";

            // Send welcome email now that the account is confirmed
            var welcomeEmailHtml = _emailTemplateService.GetWelcomeTemplate(
                $"{user.FirstName} {user.LastName}",
                loginLink,
                _appConfig.Value.AppName);

            await _emailSender.SendEmailAsync(
                user.Email,
                $"Welcome to {_appConfig.Value.AppName}!",
                welcomeEmailHtml);

            // Log the activity
            await _activityLogger.LogActivityAsync(
                user.Id,
                "Email Confirmed",
                "User confirmed their email address",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                true);

            _logger.LogInformation($"Email confirmed for user {user.Email}");
            response.Data = true;
            response.Message = "Email confirmed successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email");
            response.Success = false;
            response.Message = "Email confirmation failed due to server error";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto model)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Password change failed";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Revoke all refresh tokens for security
            foreach (var refreshToken in user.RefreshTokens)
            {
                refreshToken.IsRevoked = true;
            }
            await _userManager.UpdateAsync(user);

            // Send password changed notification
            var emailHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Password Changed</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .alert {{
            background-color: #D1ECF1;
            border: 1px solid #BEE5EB;
            color: #0C5460;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Password Changed</h2>
        </div>
        <p>Hi {user.FirstName} {user.LastName},</p>
        <p>Your password for {_appConfig.Value.AppName} was recently changed.</p>
        <div class='alert'>
            <p><strong>Important:</strong> If you did not make this change, please contact our support team immediately as your account may be compromised.</p>
        </div>
        <p>For security purposes, you've been logged out of all devices and will need to sign in again with your new password.</p>
        <p>Best regards,<br>The {_appConfig.Value.AppName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {_appConfig.Value.AppName}. All rights reserved.</p>
    </div>
</body>
</html>";

            await _emailSender.SendEmailAsync(
                user.Email,
                "Security Alert: Your Password Was Changed",
                emailHtml);

            // Log the activity
            await _activityLogger.LogActivityAsync(
                user.Id,
                "Password Changed",
                "User changed their password",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                true);

            _logger.LogInformation($"Password changed for user {user.Email}");
            response.Data = true;
            response.Message = "Password changed successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            response.Success = false;
            response.Message = "Password change failed due to server error";
            return response;
        }
    }

    public async Task SendAccountLockedNotificationAsync(ApplicationUser user, int lockoutMinutes)
    {
        try
        {
            if (user == null)
            {
                _logger.LogWarning("Attempted to send account locked notification to null user");
                return;
            }

            // Create the account locked notification email
            var lockoutEmailHtml = _emailTemplateService.GetAccountLockedTemplate(
                $"{user.FirstName} {user.LastName}",
                lockoutMinutes,
                _appConfig.Value.AppName);

            // Send the email notification
            await _emailSender.SendEmailAsync(
                user.Email,
                "Account Temporarily Locked",
                lockoutEmailHtml);

            // Log the activity
            await _activityLogger.LogActivityAsync(
                user.Id,
                "Account Locked",
                $"Account locked for {lockoutMinutes} minutes due to multiple failed login attempts",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                true);

            _logger.LogWarning($"Account locked notification sent to user {user.Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending account locked notification to {Email}", user.Email);
            // Silently handle errors as this is a notification method
        }
    }

    public async Task<ServiceResponse<bool>> DeleteUserAsync(string userId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Check if this is the only admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                // Count how many admins we have
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

                if (adminUsers.Count <= 1)
                {
                    response.Success = false;
                    response.Message = "Cannot delete the last admin user";
                    return response;
                }
            }

            // Option 1: Hard delete the user (not recommended for production)
            // var deleteResult = await _userManager.DeleteAsync(user);

            // Option 2: Soft delete by deactivating (recommended for production)
            // This preserves the audit trail and prevents potential data integrity issues
            user.IsActive = false;
            user.Email = $"deleted_{Guid.NewGuid()}_{user.Email}"; // Ensure email uniqueness for future registrations
            user.UserName = user.Email;
            user.NormalizedEmail = user.Email.ToUpper();
            user.NormalizedUserName = user.Email.ToUpper();
            // You might also anonymize personal data here to comply with privacy regulations

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to delete user";
                response.Errors = updateResult.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Revoke all refresh tokens
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();

            // Log activity
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var adminId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _activityLogger.LogActivityAsync(
                userId,
                "User Deleted",
                $"User account was deleted by admin (ID: {adminId})",
                ipAddress,
                true);

            _logger.LogInformation("Admin (ID: {AdminId}) deleted user {UserId}", adminId, userId);
            response.Data = true;
            response.Message = "User deleted successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            response.Success = false;
            response.Message = "Failed to delete user due to server error";
            return response;
        }
    }
}