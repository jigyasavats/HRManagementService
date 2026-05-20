using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HRManagementService.Enums;
using Microsoft.IdentityModel.Tokens;

namespace HRManagementService.AuthService;

public class JwtService
{
    private readonly string _secretKey;
    private readonly int _expiryMinutes;

    public JwtService(string secretKey, int expiryMinutes = 30)
    {
        _secretKey = secretKey;
        _expiryMinutes = expiryMinutes;
    }

    public string GenerateToken(string alias, string name, UserRole role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("alias", alias),
            new Claim("name", name),
            new Claim("role", role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "HRManagementService",
            audience: "HRManagementService",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "HRManagementService",
                ValidateAudience = true,
                ValidAudience = "HRManagementService",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero // no grace period — expire exactly on time
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenValid(string token)
    {
        return ValidateToken(token) != null;
    }

    public string? GetClaim(string token, string claimType)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(claimType)?.Value;
    }
}
