using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TeamA.ToDo.Application.Handlers;
using TeamA.ToDo.Application.Requirements;
using TeamA.ToDo.Core.Models;

namespace TeamA.ToDo.Host.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
            .AddEntityFrameworkStores<EntityFramework.ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>("default");

        // Add JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                // Get JWT secret from environment variable or configuration
                var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                               configuration["JwtSettings:Secret"];
                
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    throw new InvalidOperationException(
                        "JWT Secret is not configured. Please set the JWT_SECRET environment variable.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero // Remove delay of token when expire
                };

                // Enable custom authentication error handling
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // Skip the default challenge response
                        context.HandleResponse();

                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = "You are not authorized to access this resource"
                        });

                        return context.Response.WriteAsync(result);
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Basic policies
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
            options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));

            // More specific policies
            options.AddPolicy("CanManageUsers", policy =>
                policy.RequireRole("Admin").RequireClaim("Permission", "ManageUsers"));

            options.AddPolicy("CanViewAllTodos", policy =>
                policy.RequireRole("Admin").RequireClaim("Permission", "ViewAllTodos"));

            options.AddPolicy("CanExportData", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.HasClaim(c => c.Type == "Permission" && c.Value == "ExportData")));

            options.AddPolicy("TodoOwnerPolicy", policy =>
                policy.Requirements.Add(new ToDoOwnerRequirement()));

            options.AddPolicy("CanViewAllExpenses", policy =>
                policy.RequireRole("Admin").RequireClaim("Permission", "ViewAllExpenses"));
        });

        // Register authorization handler
        services.AddSingleton<IAuthorizationHandler, TodoTaskOwnerAuthorizationHandler>();

        return services;
    }
}