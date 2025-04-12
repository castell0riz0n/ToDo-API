using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.Utils;
using System.Security.Claims;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.FeatureManagement;

namespace TeamA.ToDo.Application.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class FeatureRequirementAttribute : TypeFilterAttribute
    {
        public FeatureRequirementAttribute(string featureName)
            : base(typeof(FeatureRequirementFilter))
        {
            Arguments = new object[] { featureName };
        }
    }

    public class FeatureRequirementFilter : IAsyncActionFilter
    {
        private readonly string _featureName;
        private readonly IFeatureManagementService _featureService;

        public FeatureRequirementFilter(
            string featureName,
            IFeatureManagementService featureService)
        {
            _featureName = featureName;
            _featureService = featureService;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Get the user ID from claims
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if the feature is enabled for this user (this method now includes time and role checks)
            var isEnabled = await _featureService.IsFeatureEnabledAsync(_featureName, userId);

            if (!isEnabled)
            {
                var response = new ServiceResponse<object>
                {
                    Success = false,
                    Message = $"The {_featureName} feature is not available for your account."
                };

                context.Result = new ObjectResult(response)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // Feature is enabled, continue processing
            await next();
        }
    }
}