namespace TeamA.ToDo.Core.Models.FeatureManagement;

public class FeatureDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool EnabledByDefault { get; set; } = true;

    // Time-based properties
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }

    // Navigation properties
    public ICollection<UserFeatureFlag> UserFeatureFlags { get; set; } = new List<UserFeatureFlag>();
    public ICollection<RoleFeatureAccess> RoleFeatureAccess { get; set; } = new List<RoleFeatureAccess>();
}