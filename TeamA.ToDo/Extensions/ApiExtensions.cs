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
            // Global rate limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Rate limit based on authenticated user or IP address
                var user = context.User?.Identity?.Name;
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var partitionKey = !string.IsNullOrEmpty(user) ? $"user_{user}" : $"ip_{ipAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100, // 100 requests
                        Window = TimeSpan.FromMinutes(1), // per minute
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    });
            });

            // Authentication endpoints - strict limits
            options.AddPolicy("AuthLimit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter($"auth_{ipAddress}",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5, // 5 requests
                        Window = TimeSpan.FromMinutes(15), // per 15 minutes
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queuing for auth endpoints
                    });
            });

            // Password reset endpoints - very strict limits
            options.AddPolicy("PasswordResetLimit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter($"reset_{ipAddress}",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 3, // 3 requests
                        Window = TimeSpan.FromHours(1), // per hour
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Registration endpoints
            options.AddPolicy("RegistrationLimit", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter($"register_{ipAddress}",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 3, // 3 registrations
                        Window = TimeSpan.FromHours(24), // per day
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // API endpoints for authenticated users - more generous limits
            options.AddPolicy("ApiUserLimit", context =>
            {
                var user = context.User?.Identity?.Name;
                if (string.IsNullOrEmpty(user))
                {
                    // Fall back to IP-based limiting for unauthenticated requests
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter($"api_ip_{ipAddress}",
                        partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 50,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 5
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter($"api_user_{user}",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 200, // 200 requests for authenticated users
                        Window = TimeSpan.FromMinutes(1), // per minute
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    });
            });

            // Configure what happens when rate limiting is triggered
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                // Add retry-after header
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.Add("Retry-After", 
                        ((int)retryAfter.TotalSeconds).ToString());
                }

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

        // 2. HTTPS Redirection and HSTS
        if (!env.IsDevelopment())
        {
            // Force HTTPS in production
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        else
        {
            // Optional HTTPS redirection in development
            var httpsPort = configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT");
            if (httpsPort.HasValue)
            {
                app.UseHttpsRedirection();
            }
        }

        // 3. Security Headers
        app.Use(async (context, next) =>
        {
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Remove server header
            context.Response.Headers.Remove("Server");
            
            await next();
        });

        // 4. CORS - early in pipeline for preflight requests
        app.UseCors("AllowedOrigins");

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