using System.Security.Claims;
using TeamA.ToDo.Core.Models;

namespace TeamA.ToDo.Host.Middleware;

public class PermissionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionAuthorizationMiddleware> _logger;
    private readonly Dictionary<string, List<PermissionRequirement>> _routePermissions;

    public PermissionAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<PermissionAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        // Define route-based permissions
        _routePermissions = new Dictionary<string, List<PermissionRequirement>>(StringComparer.OrdinalIgnoreCase)
            {
        // User management routes
        {
            "/api/users",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin" }, new List<string> { "ViewUsers" }),
                new PermissionRequirement(HttpMethod.Post, new List<string> { "Admin" }, new List<string> { "CreateUsers" }),
            }
        },
        {
            "/api/users/{id}",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin" }, new List<string> { "ViewUsers" }),
                new PermissionRequirement(HttpMethod.Put, new List<string> { "Admin" }, new List<string> { "UpdateUsers" }),
                new PermissionRequirement(HttpMethod.Delete, new List<string> { "Admin" }, new List<string> { "DeleteUsers" }),
            }
        },
        
        // Role management routes
        {
            "/api/roles",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin" }, new List<string> { "ViewRoles" }),
                new PermissionRequirement(HttpMethod.Post, new List<string> { "Admin" }, new List<string> { "ManageRoles" }),
            }
        },
        {
            "/api/roles/{id}",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin" }, new List<string> { "ViewRoles" }),
                new PermissionRequirement(HttpMethod.Put, new List<string> { "Admin" }, new List<string> { "ManageRoles" }),
                new PermissionRequirement(HttpMethod.Delete, new List<string> { "Admin" }, new List<string> { "ManageRoles" }),
            }
        },
        
        // Tasks routes - more permissive
        {
            "/api/tasks",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin", "User" }, new List<string>()),
                new PermissionRequirement(HttpMethod.Post, new List<string> { "Admin", "User" }, new List<string>()),
            }
        },
        {
            "/api/tasks/{id}",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin", "User" }, new List<string>()),
                new PermissionRequirement(HttpMethod.Put, new List<string> { "Admin", "User" }, new List<string>()),
                new PermissionRequirement(HttpMethod.Delete, new List<string> { "Admin", "User" }, new List<string>()),
                new PermissionRequirement(HttpMethod.Patch, new List<string> { "Admin", "User" }, new List<string>()),
            }
        },
        
        // Task-related nested routes
        {
            "/api/tasks/{taskId}/notes",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin", "User" }, new List<string>()),
                new PermissionRequirement(HttpMethod.Post, new List<string> { "Admin", "User" }, new List<string>()),
            }
        },
        {
            "/api/tasks/{taskId}/reminders",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin", "User" }, new List<string>()),
                new PermissionRequirement(HttpMethod.Post, new List<string> { "Admin", "User" }, new List<string>()),
            }
        },
        
        // Admin-only routes
        {
            "/api/tasks/all",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin" }, new List<string> { "ViewAllTodos" }),
            }
        },
        {
            "/api/admin/statistics",
            new List<PermissionRequirement>
            {
                new PermissionRequirement(HttpMethod.Get, new List<string> { "Admin" }, new List<string>()),
            }
        },
    };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip middleware for non-API routes
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Skip middleware for auth-related endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        // Skip if user is not authenticated
        if (!context.User.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Authentication required"
            });
            return;
        }

        // Find the matching route template
        string routeTemplate = FindMatchingRouteTemplate(context.Request.Path);
        if (routeTemplate == null)
        {
            // No specific permission requirements, continue to next middleware
            await _next(context);
            return;
        }

        // Find the permission requirement for this route and HTTP method
        var requirement = _routePermissions[routeTemplate]
            .FirstOrDefault(r => r.Method.Method.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase));

        if (requirement == null)
        {
            // No specific permission requirements for this method, continue to next middleware
            await _next(context);
            return;
        }

        // Check if user has the required role
        bool hasRequiredRole = false;

        if (requirement.Roles.Any())
        {
            foreach (var role in requirement.Roles)
            {
                if (context.User.IsInRole(role))
                {
                    hasRequiredRole = true;
                    break;
                }
            }
        }
        else
        {
            // No specific roles required
            hasRequiredRole = true;
        }

        // Check if user has the required permission
        bool hasRequiredPermission = false;

        if (requirement.Permissions.Any())
        {
            foreach (var permission in requirement.Permissions)
            {
                if (context.User.HasClaim(c => c.Type == "Permission" && c.Value == permission))
                {
                    hasRequiredPermission = true;
                    break;
                }
            }
        }
        else
        {
            // No specific permissions required
            hasRequiredPermission = true;
        }

        // Check if user meets the authorization requirements
        if (hasRequiredRole && (hasRequiredPermission || !requirement.Permissions.Any()))
        {
            await _next(context);
        }
        else
        {
            _logger.LogWarning(
                "Access denied for {User} to {Path} with method {Method}",
                context.User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown",
                context.Request.Path,
                context.Request.Method);

            context.Response.StatusCode = 403; // Forbidden
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "You don't have permission to access this resource"
            });
        }
    }

    private string FindMatchingRouteTemplate(PathString path)
    {
        // Convert path to string without starting slash for easier comparison
        string requestPath = path.Value?.TrimStart('/');
        if (string.IsNullOrEmpty(requestPath))
        {
            return null;
        }

        // First, try direct match
        string fullPath = "/" + requestPath;
        if (_routePermissions.ContainsKey(fullPath))
        {
            return fullPath;
        }

        // Then, try to match route templates with parameters
        foreach (var routeTemplate in _routePermissions.Keys)
        {
            if (MatchesRouteTemplate(fullPath, routeTemplate))
            {
                return routeTemplate;
            }
        }

        return null;
    }

    private bool MatchesRouteTemplate(string requestPath, string routeTemplate)
    {
        // Split both paths into segments
        var requestSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var templateSegments = routeTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Quick check - if they have different number of segments, they can't match
        if (requestSegments.Length != templateSegments.Length)
        {
            return false;
        }

        // Check each segment
        for (int i = 0; i < templateSegments.Length; i++)
        {
            var templateSegment = templateSegments[i];
            var requestSegment = requestSegments[i];

            // If template segment is a parameter (enclosed in {}), it's a match
            if (templateSegment.StartsWith("{") && templateSegment.EndsWith("}"))
            {
                continue;
            }

            // Otherwise, segments must match exactly
            if (!string.Equals(templateSegment, requestSegment, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}