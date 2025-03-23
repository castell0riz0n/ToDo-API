using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.DTOs.Users;

namespace TeamA.ToDo.Application.Interfaces;

public interface IUserService
{
    Task<ServiceResponse<PagedResponse<UserProfileDto>>> GetAllUsersAsync(PaginationParams paginationParams);
    Task<ServiceResponse<string>> RegisterUserAsync(UserRegistrationDto model);
    Task<ServiceResponse<AuthResponseDto>> LoginAsync(LoginDto model);
    Task<ServiceResponse<string>> ForgotPasswordAsync(string email);
    Task<ServiceResponse<string>> ResetPasswordAsync(ResetPasswordDto model);
    Task<ServiceResponse<UserProfileDto>> GetUserProfileAsync(string userId);
    Task<ServiceResponse<UserProfileDto>> UpdateUserProfileAsync(string userId, UserUpdateDto model);
    Task<ServiceResponse<UserProfileDto>> AdminUpdateUserAsync(string userId, AdminUserUpdateDto model);
    Task<ServiceResponse<bool>> ConfirmEmailAsync(string userId, string token);
    Task<ServiceResponse<bool>> ChangePasswordAsync(string userId, ChangePasswordDto model);
    Task<ServiceResponse<bool>> DeleteUserAsync(string userId);
}