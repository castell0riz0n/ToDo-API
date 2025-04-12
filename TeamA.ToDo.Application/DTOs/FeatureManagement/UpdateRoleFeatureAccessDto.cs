using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.FeatureManagement;

public class UpdateRoleFeatureAccessDto
{
    [Required]
    public bool IsEnabled { get; set; }
}