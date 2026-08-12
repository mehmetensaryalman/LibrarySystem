using System.Security.Claims;
using System.Text;
using LibrarySystem.Application.Interfaces.Auth;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LibrarySystem.Infrastructure.Services.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtTokenService(
        string key,
        string issuer,
        string audience,
        int expirationMinutes)
    {
        _key = key;
        _issuer = issuer;
        _audience = audience;
        _expirationMinutes = expirationMinutes;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(
        string userId,
        string email,
        IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAt =
            now.AddMinutes(_expirationMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                userId),

            new(
                ClaimTypes.NameIdentifier,
                userId),

            new(
                JwtRegisteredClaimNames.Email,
                email),

            new(
                ClaimTypes.Email,
                email),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_key));

        var signingCredentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var tokenDescriptor =
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _issuer,
                Audience = _audience,
                NotBefore = now,
                Expires = expiresAt,
                SigningCredentials = signingCredentials
            };

        var tokenHandler =
            new JsonWebTokenHandler();

        var token =
            tokenHandler.CreateToken(
                tokenDescriptor);

        return (token, expiresAt);
    }
}