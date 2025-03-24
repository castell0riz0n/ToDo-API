using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TeamA.ToDo.Application.Requirements;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.Application.Handlers;

public class TodoTaskOwnerAuthorizationHandler : AuthorizationHandler<ToDoOwnerRequirement, TodoTask>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ToDoOwnerRequirement requirement,
        TodoTask resource)
    {
        if (context.User == null || resource == null)
        {
            return Task.CompletedTask;
        }

        // Get user ID from claims
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Task.CompletedTask;
        }

        // Check if user is admin (admins can access all todos)
        if (context.User.IsInRole("Admin") ||
            context.User.HasClaim(c => c.Type == "Permission" && c.Value == "ViewAllTodos"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user is the owner of the todo item
        if (resource.UserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}