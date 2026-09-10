using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Application.Services;

/// <param name="ExpiresAtUtc">When the token stops being accepted.</param>
/// <param name="Role">"User" or "Admin" - lets the client show or hide the admin screens.</param>
public sealed record AuthResult(
    string Token,
    DateTime ExpiresAtUtc,
    int BusinessId,
    string BusinessName,
    string Email,
    string Role);

/// <summary>Raised when registration or login cannot proceed for a caller-visible reason.</summary>
public sealed class AuthenticationFailedException(string message) : Exception(message);

/// <summary>Application use cases for registering a business and logging it in.</summary>
public sealed class AuthService(
    IBusinessRepository businessRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    public async Task<AuthResult> RegisterAsync(
        string email,
        string password,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (await businessRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new AuthenticationFailedException("Račun s tom e-mail adresom već postoji.");
        }

        var business = new Business(normalizedEmail, passwordHasher.Hash(password), businessName);
        await businessRepository.AddAsync(business, cancellationToken);
        await businessRepository.SaveChangesAsync(cancellationToken);

        return CreateResult(business);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var business = await businessRepository.FindByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);

        // Deliberately the same message whether the account is missing or the password is
        // wrong, so the response cannot be used to enumerate registered e-mail addresses.
        if (business is null || !passwordHasher.Verify(password, business.PasswordHash))
        {
            throw new AuthenticationFailedException("Neispravna e-mail adresa ili lozinka.");
        }

        return CreateResult(business);
    }

    private AuthResult CreateResult(Business business)
    {
        var token = tokenService.CreateToken(business, out var expiresAtUtc);
        return new AuthResult(
            token, expiresAtUtc, business.Id, business.Name, business.Email, business.Role.ToString());
    }
}
