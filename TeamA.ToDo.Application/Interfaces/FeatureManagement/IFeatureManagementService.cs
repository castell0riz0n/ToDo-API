using TeamA.ToDo.Application.DTOs.FeatureManagement;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Core.Models.FeatureManagement;

namespace TeamA.ToDo.Application.Interfaces.FeatureManagement;

public interface IFeatureManagementService
{
    // Feature definitions management
    Task<ServiceResponse<List<FeatureDefinitionDto>>> GetAllFeaturesAsync();
    Task<ServiceResponse<FeatureDefinitionDto>> GetFeatureByIdAsync(Guid id);
    Task<ServiceResponse<FeatureDefinitionDto>> GetFeatureByNameAsync(string name);
    Task<ServiceResponse<FeatureDefinitionDto>> CreateFeatureAsync(CreateFeatureDefinitionDto dto);
    Task<ServiceResponse<FeatureDefinitionDto>> UpdateFeatureAsync(Guid id, UpdateFeatureDefinitionDto dto);
    Task<ServiceResponse<bool>> DeleteFeatureAsync(Guid id);

    // User feature flags management
    Task<ServiceResponse<List<UserFeatureFlagDto>>> GetUserFeatureFlagsAsync(string userId);
    Task<ServiceResponse<UserFeatureFlagDto>> GetUserFeatureFlagAsync(Guid featureId, string userId);
    Task<ServiceResponse<UserFeatureFlagDto>> SetUserFeatureFlagAsync(Guid featureId, string userId, bool isEnabled);
    Task<ServiceResponse<bool>> ResetUserFeatureFlagAsync(Guid featureId, string userId);

    // Role feature access management
    Task<ServiceResponse<List<RoleFeatureAccessDto>>> GetRoleFeatureAccessAsync(Guid featureId);
    Task<ServiceResponse<RoleFeatureAccessDto>> GetRoleFeatureAccessAsync(Guid featureId, string roleId);
    Task<ServiceResponse<RoleFeatureAccessDto>> SetRoleFeatureAccessAsync(Guid featureId, string roleId, bool isEnabled);
    Task<ServiceResponse<bool>> ResetRoleFeatureAccessAsync(Guid featureId, string roleId);

    // Check if feature is enabled based on all conditions
    Task<bool> IsFeatureEnabledAsync(string featureName, string userId);

    // Check time-based availability separately
    bool IsFeatureTimeValid(FeatureDefinition feature, DateTime? currentTime = null);

    // Check if user has access through roles
    Task<bool> HasRoleBasedAccessAsync(Guid featureId, string userId);
}