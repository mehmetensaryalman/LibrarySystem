using LibrarySystem.Application.DTOs.Auth;

namespace LibrarySystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(
        RegisterRequestDto request);

    Task<AuthResultDto> LoginAsync(
        LoginRequestDto request);

    Task<AuthResultDto> ForgotPasswordAsync(
        ForgotPasswordRequestDto request);

    Task<AuthResultDto> ResetPasswordAsync(
        ResetPasswordRequestDto request);

    Task<AuthResultDto> ChangePasswordAsync(
        string userId,
        ChangePasswordRequestDto request);
}