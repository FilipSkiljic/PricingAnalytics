namespace PricingAnalytics.Domain.Matching;

/// <summary>
/// Starting catalog of hairdresser-domain services, built from the patterns observed in
/// the real market export. Used only to seed an empty database - once seeded, the catalog
/// lives in the <c>CanonicalServices</c> table and is edited through the admin screen.
/// </summary>
public static class CanonicalServiceCatalog
{
    /// <summary>
    /// Words that mark an entry as a bundled/extended service rather than the plain
    /// base service, shared across most catalog entries below.
    /// </summary>
    private const string CommonBundleIndicators =
        "pranje, feniranje, brada, boja, bojanje, pramenovi, masaza, depilacija, paket";

    /// <summary>Freshly built instances, so the caller may persist them without sharing state.</summary>
    public static IReadOnlyList<CanonicalService> SeedData() =>
    [
        new("sisanje", "Šišanje (bez naznake spola)", "šišanje", CommonBundleIndicators),
        new("musko-sisanje", "Muško šišanje", "muško šišanje", CommonBundleIndicators),
        new("zensko-sisanje", "Žensko šišanje", "žensko šišanje", CommonBundleIndicators),
        new("djecje-sisanje", "Dječje šišanje", "dječje šišanje", CommonBundleIndicators),
        new("pranje-kose", "Pranje kose", "pranje kose"),
        new("feniranje", "Feniranje", "feniranje"),
        new("bojanje-kose", "Bojanje kose", "bojanje kose", "pramenovi"),
        new("pramenovi", "Pramenovi", "pramenovi"),
        new("uredivanje-brade", "Uređivanje brade", "uređivanje brade"),
        new("brijanje", "Brijanje", "brijanje"),
    ];
}
