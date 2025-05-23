using Microsoft.Extensions.Configuration;
using TeamA.ToDo.Application.DTOs.Auth;

namespace TeamA.ToDo.Application.Configuration;

public static class JwtConfiguration
{
    public static JwtSettings GetJwtSettings(IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings
        {
            Issuer = configuration["JwtSettings:Issuer"],
            Audience = configuration["JwtSettings:Audience"],
            ExpiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "60")
        };

        // Try to get JWT secret from environment variable first
        jwtSettings.Secret = Environment.GetEnvironmentVariable("JWT_SECRET");

        // Fallback to configuration (for backward compatibility during migration)
        if (string.IsNullOrEmpty(jwtSettings.Secret))
        {
            jwtSettings.Secret = configuration["JwtSettings:Secret"];
        }

        // Validate that we have a secret
        if (string.IsNullOrEmpty(jwtSettings.Secret))
        {
            throw new InvalidOperationException(
                "JWT Secret is not configured. Please set the JWT_SECRET environment variable or configure JwtSettings:Secret in appsettings.");
        }

        // Validate secret strength
        if (jwtSettings.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT Secret must be at least 32 characters long for security reasons.");
        }

        return jwtSettings;
    }
}
