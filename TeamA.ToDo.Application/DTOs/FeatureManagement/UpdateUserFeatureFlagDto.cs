using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.FeatureManagement;

public class UpdateUserFeatureFlagDto
{
    [Required]
    public bool IsEnabled { get; set; }
}