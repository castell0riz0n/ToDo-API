using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models;

namespace TeamA.ToDo.Controllers;

// Controllers/AuthController.cs
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService,
        ITwoFactorService twoFactorService,
        ITokenService tokenService,
        ILogger<AuthController> logger, UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _twoFactorService = twoFactorService;
        _tokenService = tokenService;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<string>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _userService.RegisterUserAsync(model);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<AuthResponseDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        // Use the custom login method that handles lockout notifications
        var response = await _userService.LoginAsync(model);

        if (!response.Success)
        {
            // If the error message indicates a lockout, return a 429 (Too Many Requests)
            // instead of a standard 400 to better indicate the temporary lockout status
            if (response.Message.Contains("temporarily locked"))
            {
                return StatusCode(429, response);
            }

            return BadRequest(response);
        }

        // Check if user has 2FA enabled
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return Ok(new ServiceResponse<TwoFactorLoginResponseDto>
            {
                Success = true,
                Data = new TwoFactorLoginResponseDto
                {
                    RequiresTwoFactor = true,
                    UserId = user.Id
                },
                Message = "Two-factor authentication required"
            });
        }

        return Ok(response);
    }

    [HttpPost("two-factor-login")]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TwoFactorLogin([FromBody] TwoFactorLoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<AuthResponseDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _twoFactorService.LoginWithTwoFactorAsync(model.UserId, model.VerificationCode);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("recovery-code-login")]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecoveryCodeLogin([FromBody] RecoveryCodeLoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<AuthResponseDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _twoFactorService.LoginWithRecoveryCodeAsync(model.UserId, model.RecoveryCode);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<AuthResponseDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _tokenService.RefreshTokenAsync(model.Token, model.RefreshToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenDto model)
    {
        if (string.IsNullOrEmpty(model.RefreshToken))
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "Token is required"
            });
        }

        var response = await _tokenService.RevokeTokenAsync(model.RefreshToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<string>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _userService.ForgotPasswordAsync(model.Email);
        return Ok(response);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<string>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _userService.ResetPasswordAsync(model);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = "User ID and token are required"
            });
        }

        var response = await _userService.ConfirmEmailAsync(userId, token);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}