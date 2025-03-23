using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Security;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.General;

namespace TeamA.ToDo.Application.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<TwoFactorService> _logger;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOptions<AppConfig> _appConfig;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IActivityLogger _activityLogger;
    private readonly IEmailSender _emailSender;

    public TwoFactorService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<TwoFactorService> logger, IEmailTemplateService emailTemplateService, IOptions<AppConfig> appConfig, IHttpContextAccessor httpContextAccessor, IActivityLogger activityLogger, IEmailSender emailSender)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
        _emailTemplateService = emailTemplateService;
        _appConfig = appConfig;
        _httpContextAccessor = httpContextAccessor;
        _activityLogger = activityLogger;
        _emailSender = emailSender;
    }

    public async Task<ServiceResponse<TwoFactorSetupDto>> GetTwoFactorSetupInfoAsync(string userId)
    {
        var response = new ServiceResponse<TwoFactorSetupDto>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Load the authenticator key & QR code URI
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var email = await _userManager.GetEmailAsync(user);
            var authenticatorUri = GenerateQrCodeUri(email, unformattedKey);

            // Generate QR code as Base64 image
            var qrCodeBase64 = GenerateQrCodeAsBase64(authenticatorUri);

            response.Data = new TwoFactorSetupDto
            {
                SharedKey = FormatKey(unformattedKey),
                AuthenticatorUri = authenticatorUri,
                QrCodeBase64 = qrCodeBase64
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting two-factor setup info");
            response.Success = false;
            response.Message = "Failed to get two-factor setup information";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> EnableTwoFactorAuthAsync(string userId, string verificationCode)
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

            // Verify the code
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode);

            if (!isValid)
            {
                response.Success = false;
                response.Message = "Verification code is invalid";
                return response;
            }

            // Enable 2FA
            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to enable two-factor authentication";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Send 2FA enabled email
            var twoFactorEmailHtml = _emailTemplateService.GetTwoFactorEnabledTemplate(
                $"{user.FirstName} {user.LastName}",
                _appConfig.Value.AppName);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Two-Factor Authentication Enabled",
                twoFactorEmailHtml);

            // Log the activity
            await _activityLogger.LogActivityAsync(
                user.Id,
                "2FA Enabled",
                "Two-factor authentication was enabled for user account",
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                true);

            _logger.LogInformation($"Two-factor authentication enabled for user {user.Email}");
            response.Data = true;
            response.Message = "Two-factor authentication has been enabled";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling two-factor authentication");
            response.Success = false;
            response.Message = "Failed to enable two-factor authentication";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> DisableTwoFactorAuthAsync(string userId, string verificationCode)
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

            // Check if 2FA is enabled
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                response.Success = false;
                response.Message = "Two-factor authentication is not enabled";
                return response;
            }

            // Verify the code
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode);

            if (!isValid)
            {
                response.Success = false;
                response.Message = "Verification code is invalid";
                return response;
            }

            // Disable 2FA
            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to disable two-factor authentication";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Reset the authenticator key
            await _userManager.ResetAuthenticatorKeyAsync(user);

            _logger.LogInformation($"Two-factor authentication disabled for user {user.Email}");
            response.Data = true;
            response.Message = "Two-factor authentication has been disabled";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication");
            response.Success = false;
            response.Message = "Failed to disable two-factor authentication";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> ValidateTwoFactorCodeAsync(string userId, string verificationCode)
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

            // Verify the code
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode);

            response.Data = isValid;

            if (!isValid)
            {
                response.Success = false;
                response.Message = "Verification code is invalid";
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating two-factor code");
            response.Success = false;
            response.Message = "Failed to validate two-factor code";
            return response;
        }
    }

    public async Task<ServiceResponse<IEnumerable<string>>> GenerateRecoveryCodesAsync(string userId)
    {
        var response = new ServiceResponse<IEnumerable<string>>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Check if 2FA is enabled
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                response.Success = false;
                response.Message = "Two-factor authentication is not enabled";
                return response;
            }

            // Generate recovery codes
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            _logger.LogInformation($"Recovery codes generated for user {user.Email}");
            response.Data = recoveryCodes;
            response.Message = "Recovery codes generated successfully. Store these in a safe place.";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recovery codes");
            response.Success = false;
            response.Message = "Failed to generate recovery codes";
            return response;
        }
    }

    public async Task<ServiceResponse<AuthResponseDto>> LoginWithTwoFactorAsync(string userId, string verificationCode)
    {
        var response = new ServiceResponse<AuthResponseDto>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Verify the 2FA code
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode);

            if (!isValid)
            {
                response.Success = false;
                response.Message = "Invalid verification code";
                return response;
            }

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

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

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

            _logger.LogInformation($"User {user.Email} logged in with two-factor authentication");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during two-factor login");
            response.Success = false;
            response.Message = "Two-factor login failed";
            return response;
        }
    }

    public async Task<ServiceResponse<AuthResponseDto>> LoginWithRecoveryCodeAsync(string userId, string recoveryCode)
    {
        var response = new ServiceResponse<AuthResponseDto>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Verify the recovery code
            var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Invalid recovery code";
                return response;
            }

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

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

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

            _logger.LogInformation($"User {user.Email} logged in with recovery code");
            response.Message = "You've used a recovery code to log in. Please generate new recovery codes.";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recovery code login");
            response.Success = false;
            response.Message = "Recovery code login failed";
            return response;
        }
    }

    #region Helper Methods
    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;

        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.Substring(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            WebUtility.UrlEncode("TodoApp"),
            WebUtility.UrlEncode(email),
            WebUtility.UrlEncode(unformattedKey));
    }

    private string GenerateQrCodeAsBase64(string text)
    {
        // Note: In a real application, use a QR code generation library like QRCoder
        // This is a simplified placeholder for demonstration purposes
        // Example with QRCoder:

        /*
        using QRCoder;
        using System.IO;
        using System.Drawing;
        using System.Drawing.Imaging;
        
        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        QRCode qrCode = new QRCode(qrCodeData);
        Bitmap qrCodeImage = qrCode.GetGraphic(20);
        
        using (MemoryStream ms = new MemoryStream())
        {
            qrCodeImage.Save(ms, ImageFormat.Png);
            byte[] byteImage = ms.ToArray();
            return Convert.ToBase64String(byteImage);
        }
        */

        // Return placeholder for now
        return "QR_CODE_BASE64_REPRESENTATION_WOULD_GO_HERE";
    }
    #endregion
}