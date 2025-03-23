namespace TeamA.ToDo.Application.DTOs.Auth;

public class TwoFactorSetupDto
{
    public string SharedKey { get; set; }
    public string AuthenticatorUri { get; set; }
    public string QrCodeBase64 { get; set; }
}