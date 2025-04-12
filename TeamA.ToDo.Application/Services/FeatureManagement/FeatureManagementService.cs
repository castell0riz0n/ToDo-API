using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.FeatureManagement;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.FeatureManagement;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.FeatureManagement;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.FeatureManagement;

public class FeatureManagementService : IFeatureManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<FeatureManagementService> _logger;

    public FeatureManagementService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<FeatureManagementService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public FeatureManagementService(
        ApplicationDbContext context,
        ILogger<FeatureManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResponse<List<FeatureDefinitionDto>>> GetAllFeaturesAsync()
    {
        var response = new ServiceResponse<List<FeatureDefinitionDto>>();

        try
        {
            var features = await _context.FeatureDefinitions
                .OrderBy(f => f.Name)
                .ToListAsync();

            response.Data = features.Select(MapToDto).ToList();
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving features");
            response.Success = false;
            response.Message = "Failed to retrieve features";
            return response;
        }
    }

    public async Task<ServiceResponse<FeatureDefinitionDto>> GetFeatureByIdAsync(Guid id)
    {
        var response = new ServiceResponse<FeatureDefinitionDto>();

        try
        {
            var feature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feature == null)
            {
                response.Success = false;
                response.Message = "Feature not found";
                return response;
            }

            response.Data = MapToDto(feature);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature");
            response.Success = false;
            response.Message = "Failed to retrieve feature";
            return response;
        }
    }

    public async Task<ServiceResponse<FeatureDefinitionDto>> GetFeatureByNameAsync(string name)
    {
        var response = new ServiceResponse<FeatureDefinitionDto>();

        try
        {
            var feature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Name == name);

            if (feature == null)
            {
                response.Success = false;
                response.Message = "Feature not found";
                return response;
            }

            response.Data = MapToDto(feature);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature");
            response.Success = false;
            response.Message = "Failed to retrieve feature";
            return response;
        }
    }

    public async Task<ServiceResponse<FeatureDefinitionDto>> CreateFeatureAsync(CreateFeatureDefinitionDto dto)
    {
        var response = new ServiceResponse<FeatureDefinitionDto>();

        try
        {
            // Check if feature with the same name already exists
            var existingFeature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Name == dto.Name);

            if (existingFeature != null)
            {
                response.Success = false;
                response.Message = "A feature with this name already exists";
                return response;
            }

            var feature = new FeatureDefinition
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                EnabledByDefault = dto.EnabledByDefault,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FeatureDefinitions.AddAsync(feature);
            await _context.SaveChangesAsync();

            response.Data = MapToDto(feature);
            response.Message = "Feature created successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature");
            response.Success = false;
            response.Message = "Failed to create feature";
            return response;
        }
    }

    public async Task<ServiceResponse<FeatureDefinitionDto>> UpdateFeatureAsync(Guid id, UpdateFeatureDefinitionDto dto)
    {
        var response = new ServiceResponse<FeatureDefinitionDto>();

        try
        {
            var feature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feature == null)
            {
                response.Success = false;
                response.Message = "Feature not found";
                return response;
            }

            // Check if name is being changed and if it already exists
            if (!string.IsNullOrEmpty(dto.Name) && dto.Name != feature.Name)
            {
                var existingFeature = await _context.FeatureDefinitions
                    .FirstOrDefaultAsync(f => f.Name == dto.Name && f.Id != id);

                if (existingFeature != null)
                {
                    response.Success = false;
                    response.Message = "A feature with this name already exists";
                    return response;
                }

                feature.Name = dto.Name;
            }

            if (dto.Description != null)
            {
                feature.Description = dto.Description;
            }

            if (dto.EnabledByDefault.HasValue)
            {
                feature.EnabledByDefault = dto.EnabledByDefault.Value;
            }

            feature.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            response.Data = MapToDto(feature);
            response.Message = "Feature updated successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature");
            response.Success = false;
            response.Message = "Failed to update feature";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> DeleteFeatureAsync(Guid id)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var feature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feature == null)
            {
                response.Success = false;
                response.Message = "Feature not found";
                return response;
            }

            _context.FeatureDefinitions.Remove(feature);
            await _context.SaveChangesAsync();

            response.Data = true;
            response.Message = "Feature deleted successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature");
            response.Success = false;
            response.Message = "Failed to delete feature";
            return response;
        }
    }

    public async Task<ServiceResponse<List<UserFeatureFlagDto>>> GetUserFeatureFlagsAsync(string userId)
    {
        var response = new ServiceResponse<List<UserFeatureFlagDto>>();

        try
        {
            var userFlags = await _context.UserFeatureFlags
                .Include(uff => uff.FeatureDefinition)
                .Include(uff => uff.User)
                .Where(uff => uff.UserId == userId)
                .ToListAsync();

            response.Data = userFlags.Select(MapToUserFeatureFlagDto).ToList();
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user feature flags");
            response.Success = false;
            response.Message = "Failed to retrieve user feature flags";
            return response;
        }
    }

    public async Task<ServiceResponse<UserFeatureFlagDto>> GetUserFeatureFlagAsync(Guid featureId, string userId)
    {
        var response = new ServiceResponse<UserFeatureFlagDto>();

        try
        {
            var userFlag = await _context.UserFeatureFlags
                .Include(uff => uff.FeatureDefinition)
                .Include(uff => uff.User)
                .FirstOrDefaultAsync(uff => uff.FeatureDefinitionId == featureId && uff.UserId == userId);

            if (userFlag == null)
            {
                response.Success = false;
                response.Message = "User feature flag not found";
                return response;
            }

            response.Data = MapToUserFeatureFlagDto(userFlag);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user feature flag");
            response.Success = false;
            response.Message = "Failed to retrieve user feature flag";
            return response;
        }
    }

    public async Task<ServiceResponse<UserFeatureFlagDto>> SetUserFeatureFlagAsync(Guid featureId, string userId, bool isEnabled)
    {
        var response = new ServiceResponse<UserFeatureFlagDto>();

        try
        {
            // Verify the feature exists
            var feature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Id == featureId);

            if (feature == null)
            {
                response.Success = false;
                response.Message = "Feature not found";
                return response;
            }

            // Verify the user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Find or create user feature flag
            var userFlag = await _context.UserFeatureFlags
                .FirstOrDefaultAsync(uff => uff.FeatureDefinitionId == featureId && uff.UserId == userId);

            if (userFlag == null)
            {
                userFlag = new UserFeatureFlag
                {
                    Id = Guid.NewGuid(),
                    FeatureDefinitionId = featureId,
                    UserId = userId,
                    IsEnabled = isEnabled,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.UserFeatureFlags.AddAsync(userFlag);
            }
            else
            {
                userFlag.IsEnabled = isEnabled;
                userFlag.LastModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Reload the flag with navigation properties
            userFlag = await _context.UserFeatureFlags
                .Include(uff => uff.FeatureDefinition)
                .Include(uff => uff.User)
                .FirstOrDefaultAsync(uff => uff.Id == userFlag.Id);

            response.Data = MapToUserFeatureFlagDto(userFlag);
            response.Message = "User feature flag set successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting user feature flag");
            response.Success = false;
            response.Message = "Failed to set user feature flag";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> ResetUserFeatureFlagAsync(Guid featureId, string userId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var userFlag = await _context.UserFeatureFlags
                .FirstOrDefaultAsync(uff => uff.FeatureDefinitionId == featureId && uff.UserId == userId);

            if (userFlag == null)
            {
                response.Success = false;
                response.Message = "User feature flag not found";
                return response;
            }

            _context.UserFeatureFlags.Remove(userFlag);
            await _context.SaveChangesAsync();

            response.Data = true;
            response.Message = "User feature flag reset to default successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting user feature flag");
            response.Success = false;
            response.Message = "Failed to reset user feature flag";
            return response;
        }
    }

    public async Task<ServiceResponse<List<RoleFeatureAccessDto>>> GetRoleFeatureAccessAsync(Guid featureId)
    {
        var response = new ServiceResponse<List<RoleFeatureAccessDto>>();

        try
        {
            var roleAccess = await _context.RoleFeatureAccess
                .Include(rf => rf.FeatureDefinition)
                .Include(rf => rf.Role)
                .Where(rf => rf.FeatureDefinitionId == featureId)
                .ToListAsync();

            response.Data = roleAccess.Select(rf => new RoleFeatureAccessDto
            {
                Id = rf.Id,
                FeatureDefinitionId = rf.FeatureDefinitionId,
                FeatureName = rf.FeatureDefinition.Name,
                RoleId = rf.RoleId,
                RoleName = rf.Role.Name,
                IsEnabled = rf.IsEnabled
            }).ToList();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role feature access");
            response.Success = false;
            response.Message = "Failed to retrieve role feature access";
            return response;
        }
    }

    public async Task<ServiceResponse<RoleFeatureAccessDto>> GetRoleFeatureAccessAsync(Guid featureId, string roleId)
    {
        var response = new ServiceResponse<RoleFeatureAccessDto>();

        try
        {
            var roleAccess = await _context.RoleFeatureAccess
                .Include(rf => rf.FeatureDefinition)
                .Include(rf => rf.Role)
                .FirstOrDefaultAsync(rf => rf.FeatureDefinitionId == featureId && rf.RoleId == roleId);

            if (roleAccess == null)
            {
                response.Success = false;
                response.Message = "Role feature access not found";
                return response;
            }

            response.Data = new RoleFeatureAccessDto
            {
                Id = roleAccess.Id,
                FeatureDefinitionId = roleAccess.FeatureDefinitionId,
                FeatureName = roleAccess.FeatureDefinition.Name,
                RoleId = roleAccess.RoleId,
                RoleName = roleAccess.Role.Name,
                IsEnabled = roleAccess.IsEnabled
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role feature access");
            response.Success = false;
            response.Message = "Failed to retrieve role feature access";
            return response;
        }
    }

    public async Task<ServiceResponse<RoleFeatureAccessDto>> SetRoleFeatureAccessAsync(Guid featureId, string roleId, bool isEnabled)
    {
        var response = new ServiceResponse<RoleFeatureAccessDto>();

        try
        {
            // Verify the feature exists
            var feature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Id == featureId);

            if (feature == null)
            {
                response.Success = false;
                response.Message = "Feature not found";
                return response;
            }

            // Verify the role exists
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                response.Success = false;
                response.Message = "Role not found";
                return response;
            }

            // Find or create role feature access
            var roleAccess = await _context.RoleFeatureAccess
                .FirstOrDefaultAsync(rf => rf.FeatureDefinitionId == featureId && rf.RoleId == roleId);

            if (roleAccess == null)
            {
                roleAccess = new RoleFeatureAccess
                {
                    Id = Guid.NewGuid(),
                    FeatureDefinitionId = featureId,
                    RoleId = roleId,
                    IsEnabled = isEnabled,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.RoleFeatureAccess.AddAsync(roleAccess);
            }
            else
            {
                roleAccess.IsEnabled = isEnabled;
                roleAccess.LastModifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Reload the access with navigation properties
            roleAccess = await _context.RoleFeatureAccess
                .Include(rf => rf.FeatureDefinition)
                .Include(rf => rf.Role)
                .FirstOrDefaultAsync(rf => rf.Id == roleAccess.Id);

            response.Data = new RoleFeatureAccessDto
            {
                Id = roleAccess.Id,
                FeatureDefinitionId = roleAccess.FeatureDefinitionId,
                FeatureName = roleAccess.FeatureDefinition.Name,
                RoleId = roleAccess.RoleId,
                RoleName = roleAccess.Role.Name,
                IsEnabled = roleAccess.IsEnabled
            };

            response.Message = "Role feature access set successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting role feature access");
            response.Success = false;
            response.Message = "Failed to set role feature access";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> ResetRoleFeatureAccessAsync(Guid featureId, string roleId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var roleAccess = await _context.RoleFeatureAccess
                .FirstOrDefaultAsync(rf => rf.FeatureDefinitionId == featureId && rf.RoleId == roleId);

            if (roleAccess == null)
            {
                response.Success = false;
                response.Message = "Role feature access not found";
                return response;
            }

            _context.RoleFeatureAccess.Remove(roleAccess);
            await _context.SaveChangesAsync();

            response.Data = true;
            response.Message = "Role feature access reset successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting role feature access");
            response.Success = false;
            response.Message = "Failed to reset role feature access";
            return response;
        }
    }

    // Add methods for checking feature availability
    public bool IsFeatureTimeValid(FeatureDefinition feature, DateTime? currentTime = null)
    {
        if (feature == null)
            return false;

        var now = currentTime ?? DateTime.UtcNow;

        // If no time restrictions, feature is valid time-wise
        if (!feature.AvailableFrom.HasValue && !feature.AvailableUntil.HasValue)
            return true;

        // Check start date if set
        if (feature.AvailableFrom.HasValue && now < feature.AvailableFrom.Value)
            return false;

        // Check end date if set
        if (feature.AvailableUntil.HasValue && now > feature.AvailableUntil.Value)
            return false;

        // Passed all checks
        return true;
    }

    public async Task<bool> HasRoleBasedAccessAsync(Guid featureId, string userId)
    {
        try
        {
            // Get user's roles
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Any())
                return false;

            // Convert role names to IDs
            var roleIds = await _context.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            // Check if any of the user's roles have access to this feature
            return await _context.RoleFeatureAccess
                .AnyAsync(rf => rf.FeatureDefinitionId == featureId &&
                               roleIds.Contains(rf.RoleId) &&
                               rf.IsEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role-based feature access");
            return false;
        }
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName, string userId)
    {
        try
        {
            // Get the feature definition
            var feature = await _context.FeatureDefinitions
                .FirstOrDefaultAsync(f => f.Name == featureName);

            if (feature == null)
            {
                // Feature doesn't exist, consider it disabled
                return false;
            }

            // Check time-based constraints first
            if (!IsFeatureTimeValid(feature))
            {
                // Feature is not active based on time constraints
                return false;
            }

            // Check if there's a user-specific override
            var userFlag = await _context.UserFeatureFlags
                .FirstOrDefaultAsync(uff => uff.FeatureDefinitionId == feature.Id && uff.UserId == userId);

            if (userFlag != null)
            {
                // User-specific setting overrides everything else
                return userFlag.IsEnabled;
            }

            // Check if user has role-based access
            var hasRoleAccess = await HasRoleBasedAccessAsync(feature.Id, userId);
            if (hasRoleAccess)
            {
                // User has access through role
                return true;
            }

            // If no user or role specific settings, use the default
            return feature.EnabledByDefault;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if feature is enabled");
            // In case of error, assume the feature is disabled
            return false;
        }
    }

    // Helper methods for mapping entities to DTOs
    private FeatureDefinitionDto MapToDto(FeatureDefinition feature)
    {
        return new FeatureDefinitionDto
        {
            Id = feature.Id,
            Name = feature.Name,
            Description = feature.Description,
            EnabledByDefault = feature.EnabledByDefault,
            AvailableFrom = feature.AvailableFrom,
            AvailableUntil = feature.AvailableUntil,
            IsCurrentlyAvailable = IsFeatureTimeValid(feature),
            CreatedAt = feature.CreatedAt,
            LastModifiedAt = feature.LastModifiedAt,
            EnabledForRoles = new List<string>() // Will be populated by caller
        };
    }

    private UserFeatureFlagDto MapToUserFeatureFlagDto(UserFeatureFlag userFlag)
    {
        return new UserFeatureFlagDto
        {
            Id = userFlag.Id,
            FeatureDefinitionId = userFlag.FeatureDefinitionId,
            FeatureName = userFlag.FeatureDefinition?.Name,
            UserId = userFlag.UserId,
            UserEmail = userFlag.User?.Email,
            IsEnabled = userFlag.IsEnabled
        };
    }
}