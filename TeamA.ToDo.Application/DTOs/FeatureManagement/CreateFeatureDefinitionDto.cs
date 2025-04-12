using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.FeatureManagement;

public class CreateFeatureDefinitionDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    public bool EnabledByDefault { get; set; } = true;

    // Time-based properties
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }

    // Role-based access
    public List<string> EnabledForRoles { get; set; } = new List<string>();
}