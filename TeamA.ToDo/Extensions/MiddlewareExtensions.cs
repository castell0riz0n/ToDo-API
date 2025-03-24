using TeamA.ToDo.Host.Middleware;
using TeamA.ToDo.Middleware;

namespace TeamA.ToDo.Host.Extensions;
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UsePermissionAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PermissionAuthorizationMiddleware>();
    }

    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}