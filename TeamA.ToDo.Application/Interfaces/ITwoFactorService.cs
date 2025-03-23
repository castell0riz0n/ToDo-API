using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Interfaces;

public interface ITwoFactorService
{
    Task<ServiceResponse<TwoFactorSetupDto>> GetTwoFactorSetupInfoAsync(string userId);
    Task<ServiceResponse<bool>> EnableTwoFactorAuthAsync(string userId, string verificationCode);
    Task<ServiceResponse<bool>> DisableTwoFactorAuthAsync(string userId, string verificationCode);
    Task<ServiceResponse<bool>> ValidateTwoFactorCodeAsync(string userId, string verificationCode);
    Task<ServiceResponse<IEnumerable<string>>> GenerateRecoveryCodesAsync(string userId);
    Task<ServiceResponse<AuthResponseDto>> LoginWithTwoFactorAsync(string userId, string verificationCode);
    Task<ServiceResponse<AuthResponseDto>> LoginWithRecoveryCodeAsync(string userId, string recoveryCode);
}