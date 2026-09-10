using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace PricingAnalytics.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the authenticated business id out of the token's subject claim.
    /// Only ever called from [Authorize] endpoints, where a validated token is
    /// guaranteed to be present - a missing or malformed claim means the token was
    /// issued by something other than this API, which is a bug, not a user error.
    /// </summary>
    public static int GetBusinessId(this ClaimsPrincipal principal)
    {
        // ASP.NET Core remaps "sub" to ClaimTypes.NameIdentifier by default; accept either.
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(raw, out var businessId)
            ? businessId
            : throw new InvalidOperationException("Authenticated token carries no usable business id.");
    }
}
