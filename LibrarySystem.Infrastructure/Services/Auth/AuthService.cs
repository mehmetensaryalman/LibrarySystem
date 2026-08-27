using System.Text;
using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.DTOs.Auth;
using LibrarySystem.Application.Interfaces.Auth;
using LibrarySystem.Application.Interfaces.Email;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace LibrarySystem.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private const string
        PasswordResetResponseMessage =
            "E-posta adresi sistemde kayıtlıysa parola sıfırlama bağlantısı gönderilecektir.";

    private readonly
        UserManager<ApplicationUser> _userManager;

    private readonly
        IJwtTokenService _jwtTokenService;

    private readonly
        IEmailService _emailService;

    private readonly
        IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager =
            userManager;

        _jwtTokenService =
            jwtTokenService;

        _emailService =
            emailService;

        _configuration =
            configuration;
    }

    public async Task<AuthResultDto>
        RegisterAsync(
            RegisterRequestDto request)
    {
        var email =
            request.Email.Trim();

        var existingUser =
            await _userManager
                .FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Bu e-posta adresi ile kayıtlı bir kullanıcı zaten var."
            };
        }

        var user =
            new ApplicationUser
            {
                UserName = email,
                Email = email
            };

        var result =
            await _userManager
                .CreateAsync(
                    user,
                    request.Password);

        if (!result.Succeeded)
        {
            return CreateIdentityErrorResult(
                result.Errors,
                "Kullanıcı oluşturulamadı.");
        }

        var roleResult =
            await _userManager
                .AddToRoleAsync(
                    user,
                    RoleNames.User);

        if (!roleResult.Succeeded)
        {
            await _userManager
                .DeleteAsync(user);

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

    public async Task<AuthResultDto>
        LoginAsync(
            LoginRequestDto request)
    {
        var email =
            request.Email.Trim();

        var user =
            await _userManager
                .FindByEmailAsync(email);

        if (user is null)
        {
            return CreateInvalidLoginResult();
        }

        var passwordIsValid =
            await _userManager
                .CheckPasswordAsync(
                    user,
                    request.Password);

        if (!passwordIsValid)
        {
            return CreateInvalidLoginResult();
        }

        var roles =
            await _userManager
                .GetRolesAsync(user);

        var (token, expiresAt) =
            _jwtTokenService
                .GenerateToken(
                    user.Id,
                    user.Email!,
                    roles);

        return new AuthResultDto
        {
            Success = true,
            Message =
                "Giriş başarılı.",
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthResultDto>
        ForgotPasswordAsync(
            ForgotPasswordRequestDto request)
    {
        var email =
            request.Email.Trim();

        var user =
            await _userManager
                .FindByEmailAsync(email);

        if (
            user is null ||
            string.IsNullOrWhiteSpace(
                user.Email))
        {
            return CreatePasswordResetResponse();
        }

        var token =
            await _userManager
                .GeneratePasswordResetTokenAsync(
                    user);

        var encodedToken =
            EncodeToken(token);

        var frontendUrl =
            _configuration[
                "PasswordReset:FrontendUrl"];

        if (
            string.IsNullOrWhiteSpace(
                frontendUrl))
        {
            throw new InvalidOperationException(
                "PasswordReset:FrontendUrl configuration is missing.");
        }

        var separator =
            frontendUrl.Contains(
                '?',
                StringComparison.Ordinal)
                ? "&"
                : "?";

        var resetUrl =
            $"{frontendUrl}{separator}" +
            $"email={Uri.EscapeDataString(user.Email)}" +
            $"&token={Uri.EscapeDataString(encodedToken)}";

        await _emailService
            .SendPasswordResetEmailAsync(
                user.Email,
                resetUrl);

        return CreatePasswordResetResponse();
    }

    public async Task<AuthResultDto>
        ResetPasswordAsync(
            ResetPasswordRequestDto request)
    {
        var email =
            request.Email.Trim();

        var user =
            await _userManager
                .FindByEmailAsync(email);

        if (user is null)
        {
            return CreateInvalidResetTokenResult();
        }

        var decodedToken =
            DecodeToken(request.Token);

        if (
            string.IsNullOrWhiteSpace(
                decodedToken))
        {
            return CreateInvalidResetTokenResult();
        }

        var result =
            await _userManager
                .ResetPasswordAsync(
                    user,
                    decodedToken,
                    request.NewPassword);

        if (!result.Succeeded)
        {
            if (
                result.Errors.Any(
                    error =>
                        error.Code.Contains(
                            "InvalidToken",
                            StringComparison
                                .OrdinalIgnoreCase)))
            {
                return CreateInvalidResetTokenResult();
            }

            return CreateIdentityErrorResult(
                result.Errors,
                "Parola değiştirilemedi.");
        }

        return new AuthResultDto
        {
            Success = true,
            Message =
                "Parolanız başarıyla yenilendi."
        };
    }

    public async Task<AuthResultDto>
        ChangePasswordAsync(
            string userId,
            ChangePasswordRequestDto request)
    {
        if (
            string.IsNullOrWhiteSpace(
                userId))
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Kullanıcı bilgisi bulunamadı."
            };
        }

        if (
            request.NewPassword !=
            request.ConfirmNewPassword)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Yeni parola ile parola tekrarı uyuşmuyor."
            };
        }

        var user =
            await _userManager
                .FindByIdAsync(userId);

        if (user is null)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Kullanıcı bulunamadı."
            };
        }

        var currentPasswordIsValid =
            await _userManager
                .CheckPasswordAsync(
                    user,
                    request.CurrentPassword);

        if (!currentPasswordIsValid)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Mevcut parolanız hatalı."
            };
        }

        var newPasswordIsCurrentPassword =
            await _userManager
                .CheckPasswordAsync(
                    user,
                    request.NewPassword);

        if (newPasswordIsCurrentPassword)
        {
            return new AuthResultDto
            {
                Success = false,
                Message =
                    "Yeni parola mevcut parolanızdan farklı olmalıdır."
            };
        }

        var result =
            await _userManager
                .ChangePasswordAsync(
                    user,
                    request.CurrentPassword,
                    request.NewPassword);

        if (!result.Succeeded)
        {
            return CreateIdentityErrorResult(
                result.Errors,
                "Parola değiştirilemedi.");
        }

        return new AuthResultDto
        {
            Success = true,
            Message =
                "Parolanız başarıyla değiştirildi. Yeni parolanızla tekrar giriş yapabilirsiniz."
        };
    }

    private static AuthResultDto
        CreateInvalidLoginResult()
    {
        return new AuthResultDto
        {
            Success = false,
            Message =
                "E-posta veya parola hatalı."
        };
    }

    private static AuthResultDto
        CreatePasswordResetResponse()
    {
        return new AuthResultDto
        {
            Success = true,
            Message =
                PasswordResetResponseMessage
        };
    }

    private static AuthResultDto
        CreateInvalidResetTokenResult()
    {
        return new AuthResultDto
        {
            Success = false,
            Message =
                "Parola sıfırlama bağlantısı geçersiz veya süresi dolmuş."
        };
    }

    private static AuthResultDto
        CreateIdentityErrorResult(
            IEnumerable<IdentityError> errors,
            string fallbackMessage)
    {
        var errorMessages =
            errors
                .Select(
                    error =>
                        error.Description)
                .Where(
                    description =>
                        !string.IsNullOrWhiteSpace(
                            description))
                .ToArray();

        return new AuthResultDto
        {
            Success = false,
            Message =
                errorMessages.Length > 0
                    ? string.Join(
                        " ",
                        errorMessages)
                    : fallbackMessage
        };
    }

    private static string
        EncodeToken(
            string token)
    {
        var tokenBytes =
            Encoding.UTF8.GetBytes(
                token);

        return Convert
            .ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string?
        DecodeToken(
            string encodedToken)
    {
        try
        {
            var base64 =
                encodedToken
                    .Replace('-', '+')
                    .Replace('_', '/');

            var padding =
                base64.Length % 4;

            if (padding > 0)
            {
                base64 +=
                    new string(
                        '=',
                        4 - padding);
            }

            var tokenBytes =
                Convert.FromBase64String(
                    base64);

            return Encoding.UTF8
                .GetString(tokenBytes);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}