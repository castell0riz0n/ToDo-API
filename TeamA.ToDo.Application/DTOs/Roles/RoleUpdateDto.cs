using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Roles;

public class RoleUpdateDto
{
    [Required(ErrorMessage = "Role name is required")]
    [MaxLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
    public string Name { get; set; }

    [MaxLength(100, ErrorMessage = "Description cannot exceed 100 characters")]
    public string Description { get; set; }
}