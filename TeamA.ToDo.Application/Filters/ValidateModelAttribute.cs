using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var response = new ServiceResponse<object>
            {
                Success = false,
                Message = "Invalid model state",
                Errors = errors
            };

            context.Result = new BadRequestObjectResult(response);
        }
    }
}