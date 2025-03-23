namespace TeamA.ToDo.Application.DTOs.Auth;

public class TwoFactorLoginResponseDto
{
    public bool RequiresTwoFactor { get; set; }
    public string UserId { get; set; }
}