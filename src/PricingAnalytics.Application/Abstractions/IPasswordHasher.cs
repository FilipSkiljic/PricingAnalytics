namespace PricingAnalytics.Application.Abstractions;

/// <summary>
/// Port for password hashing. Keeps the specific algorithm (BCrypt) an Infrastructure
/// concern, so the Application layer never references a crypto library directly.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
