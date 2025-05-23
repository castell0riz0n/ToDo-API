using Microsoft.Extensions.Configuration;

namespace TeamA.ToDo.Application.Configuration;

public static class AdminConfiguration
{
    public static AdminSettings GetAdminSettings(IConfiguration configuration)
    {
        var settings = new AdminSettings();

        // Try to get admin credentials from environment variables first
        settings.DefaultAdminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        settings.DefaultAdminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        // Fallback to configuration for email only (never for password)
        if (string.IsNullOrEmpty(settings.DefaultAdminEmail))
        {
            settings.DefaultAdminEmail = configuration["AppConfig:DefaultAdminEmail"] ?? "admin@todo.com";
        }

        // If no password is set, generate a secure random one
        if (string.IsNullOrEmpty(settings.DefaultAdminPassword))
        {
            settings.DefaultAdminPassword = GenerateSecurePassword();
            settings.IsGeneratedPassword = true;
        }

        return settings;
    }

    public static string GenerateSecurePassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var random = new Random();
        var password = new List<char>();

        // Ensure at least one of each required character type
        password.Add(upperCase[random.Next(upperCase.Length)]);
        password.Add(lowerCase[random.Next(lowerCase.Length)]);
        password.Add(digits[random.Next(digits.Length)]);
        password.Add(special[random.Next(special.Length)]);

        // Fill the rest of the password
        var allChars = upperCase + lowerCase + digits + special;
        for (int i = password.Count; i < 16; i++)
        {
            password.Add(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password
        for (int i = password.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password.ToArray());
    }
}

public class AdminSettings
{
    public string DefaultAdminEmail { get; set; }
    public string DefaultAdminPassword { get; set; }
    public bool IsGeneratedPassword { get; set; }
}
