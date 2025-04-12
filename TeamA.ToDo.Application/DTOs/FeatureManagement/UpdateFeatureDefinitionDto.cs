using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.FeatureManagement;

public class UpdateFeatureDefinitionDto
{
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    public bool? EnabledByDefault { get; set; }

    // Time-based properties
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public bool? ClearTimeRestrictions { get; set; }
}