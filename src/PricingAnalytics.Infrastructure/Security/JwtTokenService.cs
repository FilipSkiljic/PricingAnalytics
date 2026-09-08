using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Security;

/// <summary>
/// Issues signed JWTs carrying the business identity. Stateless by design: the API
/// validates the signature on each request instead of keeping server-side sessions.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public string CreateToken(Business business, out DateTime expiresAtUtc)
    {
        expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, business.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, business.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("businessName", business.Name),
            // ClaimTypes.Role is what [Authorize(Roles = ...)] reads by default.
            new Claim(ClaimTypes.Role, business.Role.ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
