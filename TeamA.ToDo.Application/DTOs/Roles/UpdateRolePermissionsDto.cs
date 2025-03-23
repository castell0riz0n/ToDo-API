using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Roles;

public class UpdateRolePermissionsDto
{
    [Required(ErrorMessage = "Permission IDs are required")]
    public List<string> PermissionIds { get; set; }
}