using System.Net;
using System.Text.RegularExpressions;

namespace TeamA.ToDo.Application.Security;

public static class XssProtectionService
{
    // Sanitize input to prevent XSS attacks
    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // HTML encode the input to neutralize potential script tags
        input = WebUtility.HtmlEncode(input);

        return input;
    }

    // Detect potential XSS attempts
    public static bool ContainsXss(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Check for common XSS vectors
        var normalizedInput = input.ToLower();

        // Script tags
        if (normalizedInput.Contains("<script") || normalizedInput.Contains("</script>"))
            return true;

        // On* event handlers
        if (Regex.IsMatch(normalizedInput, "\\bon\\w+\\s*="))
            return true;

        // JavaScript URLs
        if (normalizedInput.Contains("javascript:"))
            return true;

        // Data URLs
        if (normalizedInput.Contains("data:text/html"))
            return true;

        // Expression or eval
        if (normalizedInput.Contains("expression") || normalizedInput.Contains("eval("))
            return true;

        // Other potentially dangerous tags
        if (normalizedInput.Contains("<iframe") || normalizedInput.Contains("<object") ||
            normalizedInput.Contains("<embed") || normalizedInput.Contains("<form"))
            return true;

        return false;
    }

    // Convert potential XSS to safe HTML (for cases where HTML is allowed)
    public static string SanitizeHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        // This is a simple placeholder - in production you should use a 
        // library like HtmlSanitizer, AngleSharp, or HtmlAgilityPack
        // Example with HtmlSanitizer:

        /*
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.Add("p");
        sanitizer.AllowedTags.Add("br");
        sanitizer.AllowedTags.Add("strong");
        sanitizer.AllowedTags.Add("em");
        sanitizer.AllowedTags.Add("ul");
        sanitizer.AllowedTags.Add("ol");
        sanitizer.AllowedTags.Add("li");
        
        return sanitizer.Sanitize(html);
        */

        // Simplified version for demonstration
        var result = html;
        result = Regex.Replace(result, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<iframe[^>]*>.*?</iframe>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<object[^>]*>.*?</object>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<embed[^>]*>.*?</embed>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"<form[^>]*>.*?</form>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        result = Regex.Replace(result, @"javascript:", "", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"on\w+\s*=", "data-removed=", RegexOptions.IgnoreCase);

        return result;
    }
}