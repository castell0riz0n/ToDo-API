using System.Threading.RateLimiting;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Filters;

namespace TeamA.ToDo.Host.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add(typeof(ValidateModelAttribute));
        });

        // Configure API Documentation
        services.AddOpenApi();

        // Configure Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Rate limit based on IP address
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100, // 100 requests
                        Window = TimeSpan.FromMinutes(1) // per minute
                    });
            });

            // Add specific limiters for sensitive routes
            options.AddPolicy("AuthLimit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10, // 10 requests
                        Window = TimeSpan.FromMinutes(5) // per 5 minutes
                    });
            });

            // Configure what happens when rate limiting is triggered
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new ServiceResponse<object>
                {
                    Success = false,
                    Message = "Too many requests. Please try again later."
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, token);
            };
        });

        // Configure CORS
        services.AddCorsConfiguration(configuration);

        return services;
    }

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var corsPolicy = "AllowedOrigins";

        services.AddCors(options =>
        {
            options.AddPolicy(corsPolicy, policy =>
            {
                policy.WithOrigins(
                        "https://yourdomain.com",
                        "https://www.yourdomain.com")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseApiConfiguration(this IApplicationBuilder app, IConfiguration configuration, IWebHostEnvironment env)
    {
        // 1. Apply Global Exception Handling - must be first!
        app.UseGlobalExceptionHandling();

        if (!env.IsDevelopment())
        {
            // 2. HSTS in production
            app.UseHsts();
        }

        // 3. CORS - early in pipeline for preflight requests
        app.UseCors("AllowedOrigins");

        // 4. HTTPS Redirection
        app.UseHttpsRedirection();

        // 5. Static Files
        app.UseStaticFiles();

        // 6. Routing - defines endpoints but doesn't execute them yet
        app.UseRouting();

        // 7. Rate Limiting - after routing but before authentication
        app.UseRateLimiter();

        // 8. Authentication must come before Authorization
        app.UseAuthentication();

        // 9. Authorization
        app.UseAuthorization();

        // 10. Custom permission middleware should come after standard Authorization
        app.UsePermissionAuthorization();


        return app;
    }
}