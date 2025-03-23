using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Auth;

public class RecoveryCodeLoginDto
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Recovery code is required")]
    public string RecoveryCode { get; set; }
}