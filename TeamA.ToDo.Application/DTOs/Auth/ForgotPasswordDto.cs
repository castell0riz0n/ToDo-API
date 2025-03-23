using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Auth;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
}