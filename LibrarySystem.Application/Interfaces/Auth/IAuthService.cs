using LibrarySystem.Application.DTOs.Auth;

namespace LibrarySystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequestDto request);

    Task<AuthResultDto> LoginAsync(LoginRequestDto request);
}