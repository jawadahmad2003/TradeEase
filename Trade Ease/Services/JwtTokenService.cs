using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TradeEase.Models.Entities;

namespace TradeEase.Services;

public interface IJwtTokenService
{
    string CreateToken(User user);
    string CreateTwoFactorPendingToken(User user);
    int? ValidateTwoFactorPendingToken(string token);
    string CreatePasswordResetToken(User user);
    int? ValidatePasswordResetToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.RoleName),
        };

        return WriteToken(claims, DateTime.UtcNow.AddHours(8));
    }

    public string CreateTwoFactorPendingToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim("purpose", "2fa_pending"),
        };
        return WriteToken(claims, DateTime.UtcNow.AddMinutes(5));
    }

    public int? ValidateTwoFactorPendingToken(string token)
    {
        return ValidatePurposeToken(token, "2fa_pending");
    }

    public string CreatePasswordResetToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim("purpose", "pwd_reset"),
        };
        return WriteToken(claims, DateTime.UtcNow.AddMinutes(15));
    }

    public int? ValidatePasswordResetToken(string token)
    {
        return ValidatePurposeToken(token, "pwd_reset");
    }

    private int? ValidatePurposeToken(string token, string purpose)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var keyValue = Environment.GetEnvironmentVariable("TRADEEASE_JWT_KEY")
                ?? _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key missing");

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "TradeEase",
                ValidAudience = _config["Jwt:Audience"] ?? "TradeEaseClient",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue)),
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            if (principal.FindFirst("purpose")?.Value != purpose) return null;
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var keyValue = Environment.GetEnvironmentVariable("TRADEEASE_JWT_KEY")
            ?? _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key missing");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "TradeEase",
            audience: _config["Jwt:Audience"] ?? "TradeEaseClient",
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
