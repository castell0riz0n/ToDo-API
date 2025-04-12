namespace TeamA.ToDo.Application.DTOs.FeatureManagement;

public class UserFeatureFlagDto
{
    public Guid Id { get; set; }
    public Guid FeatureDefinitionId { get; set; }
    public string FeatureName { get; set; }
    public string UserId { get; set; }
    public string UserEmail { get; set; }
    public bool IsEnabled { get; set; }
}