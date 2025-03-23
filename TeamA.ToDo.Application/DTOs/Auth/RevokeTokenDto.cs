using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Auth;

public class RevokeTokenDto
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; }
}