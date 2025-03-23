using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services;

public class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        ILogger<TokenService> logger)
    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.Value.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Value.Issuer,
            audience: _jwtSettings.Value.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // We don't care about the token's expiration
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Value.Issuer,
            ValidAudience = _jwtSettings.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret))
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;

        if (jwtSecurityToken == null ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    public async Task<ServiceResponse<AuthResponseDto>> RefreshTokenAsync(string token, string refreshToken)
    {
        var response = new ServiceResponse<AuthResponseDto>();

        try
        {
            var principal = GetPrincipalFromExpiredToken(token);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                response.Success = false;
                response.Message = "Invalid token";
                return response;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                return response;
            }

            // Find the refresh token
            var storedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (storedRefreshToken == null)
            {
                response.Success = false;
                response.Message = "Invalid refresh token";
                return response;
            }

            if (storedRefreshToken.ExpiryDate < DateTime.UtcNow)
            {
                response.Success = false;
                response.Message = "Refresh token expired";
                return response;
            }

            if (storedRefreshToken.IsRevoked)
            {
                // Revoke all descendant refresh tokens
                await RevokeAllRefreshTokensAsync(userId);
                response.Success = false;
                response.Message = "Refresh token revoked";
                return response;
            }

            // Generate new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateAccessToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            // Revoke the old refresh token and save the new one
            storedRefreshToken.IsRevoked = true;

            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            // Create response
            response.Data = new AuthResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _jwtSettings.Value.ExpiryMinutes * 60, // Convert to seconds
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            };

            _logger.LogInformation($"Tokens refreshed for user {user.Email}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            response.Success = false;
            response.Message = "Token refresh failed";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> RevokeTokenAsync(string refreshToken)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var storedRefreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedRefreshToken == null)
            {
                response.Success = false;
                response.Message = "Invalid refresh token";
                return response;
            }

            if (storedRefreshToken.IsRevoked)
            {
                response.Success = false;
                response.Message = "Token already revoked";
                return response;
            }

            // Revoke the token
            storedRefreshToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Refresh token revoked for user {storedRefreshToken.User.Email}");
            response.Data = true;

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            response.Success = false;
            response.Message = "Token revocation failed";
            return response;
        }
    }

    private async Task RevokeAllRefreshTokensAsync(string userId)
    {
        var userRefreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in userRefreshTokens)
        {
            token.IsRevoked = true;
        }

        await _context.SaveChangesAsync();
        _logger.LogWarning($"All refresh tokens revoked for user {userId}");
    }
}