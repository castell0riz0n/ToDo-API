using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.DTOs.Roles;
using TeamA.ToDo.Application.Interfaces;

namespace TeamA.ToDo.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "RequireAdminRole")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        IRoleService roleService,
        ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoles()
    {
        var response = await _roleService.GetAllRolesAsync();
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<RoleDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(string id)
    {
        var response = await _roleService.GetRoleByIdAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse<RoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ServiceResponse<RoleDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<RoleDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _roleService.CreateRoleAsync(model);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetRoleById), new { id = response.Data.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<RoleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<RoleDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleUpdateDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<RoleDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
            });
        }

        var response = await _roleService.UpdateRoleAsync(id, model);
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
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var response = await _roleService.DeleteRoleAsync(id);
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

    [HttpGet("{id}/users")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<UserRoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<UserRoleDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsersInRole(string id)
    {
        var response = await _roleService.GetUsersInRoleAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost("{roleId}/users/{userId}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddUserToRole(string roleId, string userId)
    {
        var response = await _roleService.AddUserToRoleAsync(userId, roleId);
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

    [HttpDelete("{roleId}/users/{userId}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserFromRole(string roleId, string userId)
    {
        var response = await _roleService.RemoveUserFromRoleAsync(userId, roleId);
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

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var response = await _roleService.GetPermissionsAsync();
        return Ok(response);
    }

    [HttpGet("{id}/permissions")]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<PermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<IEnumerable<PermissionDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRolePermissions(string id)
    {
        var response = await _roleService.GetRolePermissionsAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPut("{id}/permissions")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePermissions(string id, [FromBody] UpdateRolePermissionsDto model)
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

        var response = await _roleService.UpdateRolePermissionsAsync(id, model.PermissionIds);
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
}