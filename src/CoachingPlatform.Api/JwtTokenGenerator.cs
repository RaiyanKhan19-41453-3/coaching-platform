using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoachingPlatform.Application.Common;
using Microsoft.IdentityModel.Tokens;

namespace CoachingPlatform.Api;

public sealed class JwtTokenGenerator(IConfiguration configuration) : ITokenGenerator
{
    public GeneratedToken Generate(Guid userId, Guid tenantId, string email, IReadOnlyList<string> roles)
    {
        var key = configuration["Jwt:Key"]
                  ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "coaching-platform";
        var audience = configuration["Jwt:Audience"] ?? "coaching-web";
        var minutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var m) ? m : 30;

        var jti = Guid.NewGuid().ToString("N");
        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("email", email),
            new("tenant_id", tenantId.ToString()),
            new("jti", jti)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var token = new JwtSecurityToken(
            issuer, audience, claims,
            notBefore: DateTime.UtcNow, expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));

        return new GeneratedToken(new JwtSecurityTokenHandler().WriteToken(token), expires, jti);
    }
}
