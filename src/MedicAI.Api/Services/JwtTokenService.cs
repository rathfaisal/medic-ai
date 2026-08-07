using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MedicAI.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace MedicAI.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(User user, string displayName);
}

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CreateToken(User user, string displayName)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "MEDIC-AI";
        var audience = configuration["Jwt:Audience"] ?? "MEDIC-AI-Web";
        var expiryMinutes = configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 480;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(ClaimTypes.Name, displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
