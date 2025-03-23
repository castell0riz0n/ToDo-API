using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.DTOs.Roles;

namespace TeamA.ToDo.Application.Interfaces;

public interface IRoleService
{
    Task<ServiceResponse<IEnumerable<RoleDto>>> GetAllRolesAsync();
    Task<ServiceResponse<RoleDto>> GetRoleByIdAsync(string roleId);
    Task<ServiceResponse<RoleDto>> CreateRoleAsync(RoleCreateDto model);
    Task<ServiceResponse<RoleDto>> UpdateRoleAsync(string roleId, RoleUpdateDto model);
    Task<ServiceResponse<bool>> DeleteRoleAsync(string roleId);
    Task<ServiceResponse<IEnumerable<UserRoleDto>>> GetUsersInRoleAsync(string roleId);
    Task<ServiceResponse<bool>> AddUserToRoleAsync(string userId, string roleId);
    Task<ServiceResponse<bool>> RemoveUserFromRoleAsync(string userId, string roleId);
    Task<ServiceResponse<IEnumerable<PermissionDto>>> GetPermissionsAsync();
    Task<ServiceResponse<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(string roleId);
    Task<ServiceResponse<bool>> UpdateRolePermissionsAsync(string roleId, List<string> permissions);
}