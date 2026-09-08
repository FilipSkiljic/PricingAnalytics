using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Application.Abstractions;

/// <summary>Port for issuing access tokens. Implemented with JWT in Infrastructure.</summary>
public interface ITokenService
{
    /// <param name="expiresAtUtc">When the issued token stops being accepted.</param>
    string CreateToken(Business business, out DateTime expiresAtUtc);
}
