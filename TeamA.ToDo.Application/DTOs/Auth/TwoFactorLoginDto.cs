using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Auth;

public class TwoFactorLoginDto
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
    [RegularExpression("^[0-9]*$", ErrorMessage = "Verification code must contain only digits")]
    public string VerificationCode { get; set; }
}