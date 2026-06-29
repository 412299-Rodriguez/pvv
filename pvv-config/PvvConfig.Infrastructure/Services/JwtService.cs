using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Entities;
using PvvConfig.Infrastructure.Settings;

namespace PvvConfig.Infrastructure.Services;

public class JwtService(IOptions<JwtSettings> options) : IJwtService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
    private readonly JwtSettings _settings = options.Value;

    public (string Token, DateTime ExpiresAt) GenerateToken(Operator op)
    {
        var expiresAt = DateTime.UtcNow.Add(TokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, op.OperatorId.ToString()),
            new(ClaimTypes.Role, op.Role.ToString()),
            new("username", op.Username),
        };

        // SystemAdmins have no company; only company operators carry a companyId.
        if (op.CompanyId.HasValue)
        {
            claims.Add(new Claim("companyId", op.CompanyId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: string.IsNullOrWhiteSpace(_settings.Audience) ? null : _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
