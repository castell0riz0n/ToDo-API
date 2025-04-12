using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.FeatureManagement;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.FeatureManagement;

namespace TeamA.ToDo.Host.Controllers.Admin
{
    [ApiController]
    [Route("api/features")]
    [Authorize(Policy = "RequireAdminRole")]
    public class FeatureManagementController : ControllerBase
    {
        private readonly IFeatureManagementService _featureService;
        private readonly ILogger<FeatureManagementController> _logger;

        public FeatureManagementController(
            IFeatureManagementService featureService,
            ILogger<FeatureManagementController> logger)
        {
            _featureService = featureService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ServiceResponse<List<FeatureDefinitionDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllFeatures()
        {
            var response = await _featureService.GetAllFeaturesAsync();
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ServiceResponse<FeatureDefinitionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<FeatureDefinitionDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFeatureById(Guid id)
        {
            var response = await _featureService.GetFeatureByIdAsync(id);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ServiceResponse<FeatureDefinitionDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ServiceResponse<FeatureDefinitionDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureDefinitionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<FeatureDefinitionDto>
                {
                    Success = false,
                    Message = "Invalid input",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var response = await _featureService.CreateFeatureAsync(dto);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetFeatureById), new { id = response.Data.Id }, response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ServiceResponse<FeatureDefinitionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<FeatureDefinitionDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResponse<FeatureDefinitionDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFeature(Guid id, [FromBody] UpdateFeatureDefinitionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<FeatureDefinitionDto>
                {
                    Success = false,
                    Message = "Invalid input",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var response = await _featureService.UpdateFeatureAsync(id, dto);

            if (!response.Success)
            {
                if (response.Message.Contains("not found"))
                {
                    return NotFound(response);
                }

                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFeature(Guid id)
        {
            var response = await _featureService.DeleteFeatureAsync(id);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("users/{userId}")]
        [ProducesResponseType(typeof(ServiceResponse<List<UserFeatureFlagDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserFeatureFlags(string userId)
        {
            var response = await _featureService.GetUserFeatureFlagsAsync(userId);
            return Ok(response);
        }

        [HttpGet("{featureId}/users/{userId}")]
        [ProducesResponseType(typeof(ServiceResponse<UserFeatureFlagDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<UserFeatureFlagDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserFeatureFlag(Guid featureId, string userId)
        {
            var response = await _featureService.GetUserFeatureFlagAsync(featureId, userId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{featureId}/users/{userId}")]
        [ProducesResponseType(typeof(ServiceResponse<UserFeatureFlagDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<UserFeatureFlagDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetUserFeatureFlag(
            Guid featureId,
            string userId,
            [FromBody] UpdateUserFeatureFlagDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<UserFeatureFlagDto>
                {
                    Success = false,
                    Message = "Invalid input",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var response = await _featureService.SetUserFeatureFlagAsync(featureId, userId, dto.IsEnabled);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{featureId}/users/{userId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetUserFeatureFlag(Guid featureId, string userId)
        {
            var response = await _featureService.ResetUserFeatureFlagAsync(featureId, userId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id}/roles")]
        [ProducesResponseType(typeof(ServiceResponse<List<RoleFeatureAccessDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatureRoles(Guid id)
        {
            var response = await _featureService.GetRoleFeatureAccessAsync(id);
            return Ok(response);
        }

        [HttpGet("{featureId}/roles/{roleId}")]
        [ProducesResponseType(typeof(ServiceResponse<RoleFeatureAccessDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<RoleFeatureAccessDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleFeatureAccess(Guid featureId, string roleId)
        {
            var response = await _featureService.GetRoleFeatureAccessAsync(featureId, roleId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{featureId}/roles/{roleId}")]
        [ProducesResponseType(typeof(ServiceResponse<RoleFeatureAccessDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<RoleFeatureAccessDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetRoleFeatureAccess(
            Guid featureId,
            string roleId,
            [FromBody] UpdateRoleFeatureAccessDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<RoleFeatureAccessDto>
                {
                    Success = false,
                    Message = "Invalid input",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var response = await _featureService.SetRoleFeatureAccessAsync(featureId, roleId, dto.IsEnabled);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{featureId}/roles/{roleId}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetRoleFeatureAccess(Guid featureId, string roleId)
        {
            var response = await _featureService.ResetRoleFeatureAccessAsync(featureId, roleId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}