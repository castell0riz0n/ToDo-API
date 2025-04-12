namespace TeamA.ToDo.Core.Models.FeatureManagement;

public class UserFeatureFlag
{
    public Guid Id { get; set; }
    public Guid FeatureDefinitionId { get; set; }
    public string UserId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }

    // Navigation properties
    public FeatureDefinition FeatureDefinition { get; set; }
    public ApplicationUser User { get; set; }
}