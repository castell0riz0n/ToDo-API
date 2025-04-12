namespace TeamA.ToDo.Application.DTOs.FeatureManagement;

public class FeatureDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool EnabledByDefault { get; set; }

    // Time-based properties
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public bool IsTimeRestricted => AvailableFrom.HasValue || AvailableUntil.HasValue;
    public bool IsCurrentlyAvailable { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    // Role info
    public List<string> EnabledForRoles { get; set; } = new List<string>();
}