namespace LibrarySystem.Application.Interfaces.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(
        string userId,
        string email,
        IEnumerable<string> roles);
}