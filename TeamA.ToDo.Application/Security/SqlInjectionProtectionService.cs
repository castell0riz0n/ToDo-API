using System.Security;

namespace TeamA.ToDo.Application.Security;

public static class SqlInjectionProtectionService
{
    // Suspicious SQL patterns to check for
    private static readonly string[] SuspiciousPatterns =
    {
        "DROP TABLE", "ALTER TABLE", "TRUNCATE TABLE", "DELETE FROM",
        "INSERT INTO", "UPDATE.*SET", "SELECT.*FROM", "EXEC", "EXECUTE",
        "xp_", "--", ";", "/*", "*/", "@", "@@", "UNION", "OR 1=1"
    };

    // Validate input for SQL injection attempts
    public static bool ContainsSqlInjection(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Normalize input for better detection
        var normalizedInput = input.ToUpper().Replace(" ", "");

        // Check for suspicious patterns
        foreach (var pattern in SuspiciousPatterns)
        {
            var normalizedPattern = pattern.ToUpper().Replace(" ", "");
            if (normalizedInput.Contains(normalizedPattern))
                return true;
        }

        return false;
    }

    // Clean input to prevent SQL injection
    public static string CleanInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Replace potentially harmful characters
        input = input.Replace("'", "''"); // Escape single quotes
        input = input.Replace(";", ""); // Remove semicolons
        input = input.Replace("--", ""); // Remove comment markers
        input = input.Replace("/*", "").Replace("*/", ""); // Remove block comment markers
        input = input.Replace("xp_", ""); // Remove extended stored procedure prefixes

        return input;
    }

    // Validate parameters for SQL injection when using raw SQL queries
    public static void ValidateParameters(Dictionary<string, object> parameters)
    {
        foreach (var param in parameters)
        {
            if (param.Value is string strValue && ContainsSqlInjection(strValue))
            {
                throw new SecurityException($"Potential SQL injection detected in parameter: {param.Key}");
            }
        }
    }
}