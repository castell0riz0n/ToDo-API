using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.DTOs.Users;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models;

namespace TeamA.ToDo.Host.Controllers.Auth;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ITwoFactorService twoFactorService,
        ILogger<UsersController> logger, UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _twoFactorService = twoFactorService;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ServiceResponse<UserProfileDto>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        var response = await _userService.GetUserProfileAsync(userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UserUpdateDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<UserProfileDto>
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
            return BadRequest(new ServiceResponse<UserProfileDto>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        var response = await _userService.UpdateUserProfileAsync(userId, model);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
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

        return Ok(response);
    }

    [HttpGet("two-factor-setup")]
    [ProducesResponseType(typeof(ServiceResponse<TwoFactorSetupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<TwoFactorSetupDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTwoFactorSetup()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ServiceResponse<TwoFactorSetupDto>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        var response = await _twoFactorService.GetTwoFactorSetupInfoAsync(userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("enable-two-factor")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorVerificationDto model)
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

        var response = await _twoFactorService.EnableTwoFactorAuthAsync(userId, model.VerificationCode);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("disable-two-factor")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorVerificationDto model)
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

        var response = await _twoFactorService.DisableTwoFactorAuthAsync(userId, model.VerificationCode);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("generate-recovery-codes")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<string>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new ServiceResponse<IEnumerable<string>>
            {
                Success = false,
                Message = "User ID not found in token"
            });
        }

        var response = await _twoFactorService.GenerateRecoveryCodesAsync(userId);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // Admin-only endpoints
    [HttpGet]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(ServiceResponse<PagedResponse<UserProfileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams paginationParams)
    {
        var response = await _userService.GetAllUsersAsync(paginationParams);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(string id)
    {
        var response = await _userService.GetUserProfileAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<UserProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUserUpdateDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<UserProfileDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _userService.AdminUpdateUserAsync(id, model);
        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var response = await _userService.DeleteUserAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
}