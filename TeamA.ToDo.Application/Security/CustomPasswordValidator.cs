using Microsoft.AspNetCore.Identity;
using TeamA.ToDo.Core.Models;

namespace TeamA.ToDo.Application.Security;

public class CustomPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string password)
    {
        var errors = new List<IdentityError>();

        // Check for common passwords (this is a small sample - use a larger database in production)
        var commonPasswords = new HashSet<string>
        {
            "password", "123456", "qwerty", "admin", "welcome", "letmein", "abc123"
        };

        if (commonPasswords.Contains(password.ToLower()))
        {
            errors.Add(new IdentityError
            {
                Code = "CommonPassword",
                Description = "The password you chose is too common and easily guessable."
            });
        }

        // Check if password contains user information
        if (!string.IsNullOrEmpty(user.FirstName) &&
            password.Contains(user.FirstName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordContainsFirstName",
                Description = "Your password cannot contain your first name."
            });
        }

        if (!string.IsNullOrEmpty(user.LastName) &&
            password.Contains(user.LastName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new IdentityError
            {
                Code = "PasswordContainsLastName",
                Description = "Your password cannot contain your last name."
            });
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            var emailParts = user.Email.Split('@');
            if (emailParts.Length > 0 &&
                password.Contains(emailParts[0], StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordContainsUsername",
                    Description = "Your password cannot contain your email username."
                });
            }
        }

        // Check password strength
        var hasSequentialChars = false;
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (char.IsLetterOrDigit(password[i]) &&
                char.IsLetterOrDigit(password[i + 1]) &&
                char.IsLetterOrDigit(password[i + 2]))
            {
                // Check for sequential characters like "abc" or "123"
                if ((password[i + 1] == password[i] + 1) && (password[i + 2] == password[i] + 2))
                {
                    hasSequentialChars = true;
                    break;
                }

                // Check for sequential characters like "cba" or "321"
                if ((password[i + 1] == password[i] - 1) && (password[i + 2] == password[i] - 2))
                {
                    hasSequentialChars = true;
                    break;
                }
            }
        }

        if (hasSequentialChars)
        {
            errors.Add(new IdentityError
            {
                Code = "SequentialCharacters",
                Description = "Your password contains sequential characters. Please avoid patterns like '123' or 'abc'."
            });
        }

        return errors.Count == 0 ?
            IdentityResult.Success :
            IdentityResult.Failed(errors.ToArray());
    }
}