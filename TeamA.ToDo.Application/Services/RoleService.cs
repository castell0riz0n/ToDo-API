using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.DTOs.Roles;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResponse<IEnumerable<RoleDto>>> GetAllRolesAsync()
    {
        var response = new ServiceResponse<IEnumerable<RoleDto>>();

        try
        {
            var roles = await _roleManager.Roles.ToListAsync();
            response.Data = _mapper.Map<IEnumerable<RoleDto>>(roles);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            response.Success = false;
            response.Message = "Failed to retrieve roles";
            return response;
        }
    }

    public async Task<ServiceResponse<RoleDto>> GetRoleByIdAsync(string roleId)
    {
        var response = new ServiceResponse<RoleDto>();

        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            response.Data = _mapper.Map<RoleDto>(role);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role");
            response.Success = false;
            response.Message = "Failed to retrieve role";
            return response;
        }
    }

    public async Task<ServiceResponse<RoleDto>> CreateRoleAsync(RoleCreateDto model)
    {
        var response = new ServiceResponse<RoleDto>();

        try
        {
            // Check if role already exists
            if (await _roleManager.RoleExistsAsync(model.Name))
            {
                response.Success = false;
                response.Message = "Role already exists";
                return response;
            }

            // Create the role
            var role = new ApplicationRole
            {
                Name = model.Name,
                NormalizedName = model.Name.ToUpper(),
                Description = model.Description,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Role creation failed";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Add permissions if provided
            if (model.Permissions != null && model.Permissions.Any())
            {
                foreach (var permissionId in model.Permissions)
                {
                    var permission = await _context.Permissions.FindAsync(permissionId);
                    if (permission != null)
                    {
                        var rolePermission = new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permission.Id
                        };
                        _context.RolePermissions.Add(rolePermission);

                        // Add as a claim for easier access control
                        await _roleManager.AddClaimAsync(role, new Claim("Permission", permission.Name));
                    }
                }
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Role '{role.Name}' created successfully");
            response.Data = _mapper.Map<RoleDto>(role);
            response.Message = "Role created successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            response.Success = false;
            response.Message = "Failed to create role";
            return response;
        }
    }

    public async Task<ServiceResponse<RoleDto>> UpdateRoleAsync(string roleId, RoleUpdateDto model)
    {
        var response = new ServiceResponse<RoleDto>();

        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            // Update role properties
            role.Description = model.Description;

            // Only update name if changed and not a default role
            if (!string.Equals(role.Name, model.Name, StringComparison.OrdinalIgnoreCase) &&
                !IsDefaultRole(role.Name))
            {
                role.Name = model.Name;
                role.NormalizedName = model.Name.ToUpper();
            }

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Role update failed";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            _logger.LogInformation($"Role '{role.Name}' updated successfully");
            response.Data = _mapper.Map<RoleDto>(role);
            response.Message = "Role updated successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role");
            response.Success = false;
            response.Message = "Failed to update role";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> DeleteRoleAsync(string roleId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            // Don't allow deletion of default roles
            if (IsDefaultRole(role.Name))
            {
                response.Success = false;
                response.Message = "Cannot delete a default role";
                return response;
            }

            // Check if role has users
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            if (usersInRole.Any())
            {
                response.Success = false;
                response.Message = "Cannot delete role while it is assigned to users";
                return response;
            }

            // Delete role permissions
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(rolePermissions);
            await _context.SaveChangesAsync();

            // Delete role
            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Role deletion failed";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            _logger.LogInformation($"Role '{role.Name}' deleted successfully");
            response.Data = true;
            response.Message = "Role deleted successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role");
            response.Success = false;
            response.Message = "Failed to delete role";
            return response;
        }
    }

    public async Task<ServiceResponse<IEnumerable<UserRoleDto>>> GetUsersInRoleAsync(string roleId)
    {
        var response = new ServiceResponse<IEnumerable<UserRoleDto>>();

        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            var userRoleDtos = new List<UserRoleDto>();

            foreach (var user in usersInRole)
            {
                userRoleDtos.Add(new UserRoleDto
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}"
                });
            }

            response.Data = userRoleDtos;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users in role");
            response.Success = false;
            response.Message = "Failed to retrieve users in role";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> AddUserToRoleAsync(string userId, string roleId)
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

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            // Check if user is already in the role
            if (await _userManager.IsInRoleAsync(user, role.Name))
            {
                response.Success = false;
                response.Message = "User is already in this role";
                return response;
            }

            // Add user to role
            var result = await _userManager.AddToRoleAsync(user, role.Name);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to add user to role";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Add role permissions as claims to the user
            var permissions = await GetRolePermissionsAsync(roleId);
            if (permissions.Success && permissions.Data.Any())
            {
                foreach (var permission in permissions.Data)
                {
                    await _userManager.AddClaimAsync(user, new Claim("Permission", permission.Name));
                }
            }

            _logger.LogInformation($"User '{user.Email}' added to role '{role.Name}'");
            response.Data = true;
            response.Message = "User added to role successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to role");
            response.Success = false;
            response.Message = "Failed to add user to role";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> RemoveUserFromRoleAsync(string userId, string roleId)
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

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            // Check if user is in the role
            if (!await _userManager.IsInRoleAsync(user, role.Name))
            {
                response.Success = false;
                response.Message = "User is not in this role";
                return response;
            }

            // Remove user from role
            var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to remove user from role";
                response.Errors = result.Errors.Select(e => e.Description).ToList();
                return response;
            }

            // Remove role-specific permission claims
            var permissions = await GetRolePermissionsAsync(roleId);
            if (permissions.Success && permissions.Data.Any())
            {
                foreach (var permission in permissions.Data)
                {
                    // Check if this permission comes from other roles user has
                    var shouldKeepPermission = false;
                    var userRoles = await _userManager.GetRolesAsync(user);

                    foreach (var userRole in userRoles)
                    {
                        if (userRole != role.Name)
                        {
                            var otherRole = await _roleManager.FindByNameAsync(userRole);
                            var otherRolePermissions = await GetRolePermissionsAsync(otherRole.Id);

                            if (otherRolePermissions.Success &&
                                otherRolePermissions.Data.Any(p => p.Name == permission.Name))
                            {
                                shouldKeepPermission = true;
                                break;
                            }
                        }
                    }

                    if (!shouldKeepPermission)
                    {
                        await _userManager.RemoveClaimAsync(user, new Claim("Permission", permission.Name));
                    }
                }
            }

            _logger.LogInformation($"User '{user.Email}' removed from role '{role.Name}'");
            response.Data = true;
            response.Message = "User removed from role successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from role");
            response.Success = false;
            response.Message = "Failed to remove user from role";
            return response;
        }
    }

    public async Task<ServiceResponse<IEnumerable<PermissionDto>>> GetPermissionsAsync()
    {
        var response = new ServiceResponse<IEnumerable<PermissionDto>>();

        try
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            response.Data = _mapper.Map<IEnumerable<PermissionDto>>(permissions);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            response.Success = false;
            response.Message = "Failed to retrieve permissions";
            return response;
        }
    }

    public async Task<ServiceResponse<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(string roleId)
    {
        var response = new ServiceResponse<IEnumerable<PermissionDto>>();

        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync();

            response.Data = _mapper.Map<IEnumerable<PermissionDto>>(rolePermissions);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role permissions");
            response.Success = false;
            response.Message = "Failed to retrieve role permissions";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> UpdateRolePermissionsAsync(string roleId, List<string> permissionIds)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            // Get existing role permissions
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            // Remove existing permission claims
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in existingClaims.Where(c => c.Type == "Permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            // Remove all existing role permissions
            _context.RolePermissions.RemoveRange(existingPermissions);

            // Add new permissions
            foreach (var permissionId in permissionIds)
            {
                var permission = await _context.Permissions.FindAsync(permissionId);
                if (permission != null)
                {
                    // Add to role permissions
                    var rolePermission = new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    };
                    _context.RolePermissions.Add(rolePermission);

                    // Add as claim
                    await _roleManager.AddClaimAsync(role, new Claim("Permission", permission.Name));
                }
            }

            await _context.SaveChangesAsync();

            // Update user claims with the new permissions
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            foreach (var user in usersInRole)
            {
                // Get all permissions for all user's roles
                var userRoles = await _userManager.GetRolesAsync(user);
                var allPermissions = new HashSet<string>();

                foreach (var userRole in userRoles)
                {
                    var currentRole = await _roleManager.FindByNameAsync(userRole);
                    var rolePerms = await GetRolePermissionsAsync(currentRole.Id);

                    if (rolePerms.Success && rolePerms.Data.Any())
                    {
                        foreach (var perm in rolePerms.Data)
                        {
                            allPermissions.Add(perm.Name);
                        }
                    }
                }

                // Update user claims
                var userClaims = await _userManager.GetClaimsAsync(user);
                var permissionClaims = userClaims.Where(c => c.Type == "Permission").ToList();

                // Remove claims that are no longer in any role
                foreach (var claim in permissionClaims)
                {
                    if (!allPermissions.Contains(claim.Value))
                    {
                        await _userManager.RemoveClaimAsync(user, claim);
                    }
                }

                // Add new permission claims
                foreach (var permName in allPermissions)
                {
                    if (!permissionClaims.Any(c => c.Value == permName))
                    {
                        await _userManager.AddClaimAsync(user, new Claim("Permission", permName));
                    }
                }
            }

            _logger.LogInformation($"Permissions updated for role '{role.Name}'");
            response.Data = true;
            response.Message = "Role permissions updated successfully";

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role permissions");
            response.Success = false;
            response.Message = "Failed to update role permissions";
            return response;
        }
    }

    #region Helpers
    private bool IsDefaultRole(string roleName)
    {
        var defaultRoles = new[] { "Admin", "User" };
        return defaultRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }
    #endregion
}