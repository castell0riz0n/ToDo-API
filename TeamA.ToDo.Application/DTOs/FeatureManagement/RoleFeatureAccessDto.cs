namespace TeamA.ToDo.Application.DTOs.FeatureManagement;

public class RoleFeatureAccessDto
{
    public Guid Id { get; set; }
    public Guid FeatureDefinitionId { get; set; }
    public string FeatureName { get; set; }
    public string RoleId { get; set; }
    public string RoleName { get; set; }
    public bool IsEnabled { get; set; }
}