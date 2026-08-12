using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.DTOs.Auth;
using LibrarySystem.Application.Interfaces.Auth;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace LibrarySystem.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResultDto> RegisterAsync(
        RegisterRequestDto request)
    {
        var existingUser =
            await _userManager.FindByEmailAsync(
                request.Email);

        if (existingUser is not null)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Bu e-posta adresi ile kayıtlı bir kullanıcı zaten var."
            };
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!result.Succeeded)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = string.Join(
                    " ",
                    result.Errors.Select(
                        error => error.Description))
            };
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                RoleNames.User);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Kullanıcı rolü atanırken bir hata oluştu."
            };
        }

        return new AuthResultDto
        {
            Success = true,
            Message =
                "Kullanıcı başarıyla kaydedildi."
        };
    }

    public async Task<AuthResultDto> LoginAsync(
        LoginRequestDto request)
    {
        var user =
            await _userManager.FindByEmailAsync(
                request.Email);

        if (user is null)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "E-posta veya parola hatalı."
            };
        }

        var passwordIsValid =
            await _userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!passwordIsValid)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "E-posta veya parola hatalı."
            };
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var (token, expiresAt) =
            _jwtTokenService.GenerateToken(
                user.Id,
                user.Email!,
                roles);

        return new AuthResultDto
        {
            Success = true,
            Message = "Giriş başarılı.",
            Token = token,
            ExpiresAt = expiresAt
        };
    }
}