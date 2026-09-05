namespace PricingAnalytics.Domain.Entities;

/// <summary>
/// A registered business (obrt) that owns a price list and compares it against the market.
/// Credentials live here rather than in a separate user entity - for this application one
/// account is one business, so splitting them would add a join without adding meaning.
/// </summary>
public sealed class Business
{
    private readonly List<Service> _services = [];

    public int Id { get; private set; }

    /// <summary>Login identity. Stored lowercase so lookups are case-insensitive.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>BCrypt hash - the plaintext password is never stored or logged.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Accounts are ordinary users unless promoted. There is deliberately no way to
    /// register as an administrator - the first admin is set directly in the database.
    /// </summary>
    public BusinessRole Role { get; private set; } = BusinessRole.User;

    public IReadOnlyCollection<Service> Services => _services;

    private Business()
    {
        // EF Core materialization.
    }

    public Business(string email, string passwordHash, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Name = name.Trim();
    }

    public Service AddService(string name, decimal price)
    {
        var service = new Service(Id, name, price);
        _services.Add(service);
        return service;
    }

    public void PromoteToAdmin() => Role = BusinessRole.Admin;

    public void DemoteToUser() => Role = BusinessRole.User;
}
