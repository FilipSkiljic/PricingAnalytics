using PricingAnalytics.Application.Abstractions;

namespace PricingAnalytics.Infrastructure.Security;

/// <summary>
/// BCrypt implementation of the password hashing port. BCrypt is deliberately slow
/// and salts every hash automatically, which is what makes stolen hashes impractical
/// to brute-force - unlike a plain SHA hash.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Cost factor 12 - each increment doubles the work. High enough to be expensive
    /// for an attacker, still only tens of milliseconds for a single login.
    /// </summary>
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Stored hash is malformed (e.g. hand-edited in the database) - treat as a
            // failed login rather than crashing the request.
            return false;
        }
    }
}
