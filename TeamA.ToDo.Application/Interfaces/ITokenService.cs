using System.Security.Claims;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Core.Models;

namespace TeamA.ToDo.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    Task<ServiceResponse<AuthResponseDto>> RefreshTokenAsync(string token, string refreshToken);
    Task<ServiceResponse<bool>> RevokeTokenAsync(string refreshToken);
}