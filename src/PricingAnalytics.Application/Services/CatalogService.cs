using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;
using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Application.Services;

/// <param name="MatchedEntries">How many market entries this rule currently claims.</param>
public sealed record CatalogEntryView(
    int Id,
    string Slug,
    string DisplayName,
    string RequiredPhrase,
    string ExcludedWords,
    int MatchedEntries);

/// <param name="Occurrences">How many times this exact name appears in the corpus.</param>
public sealed record UnmatchedName(string Name, int Occurrences);

/// <summary>
/// What a proposed rule would do to the corpus, computed before it is saved.
/// The three counts partition <paramref name="WouldMatchTotal"/>.
/// </summary>
/// <param name="WouldMatchTotal">Entries the rule would claim once saved.</param>
/// <param name="CurrentlyUnmatched">Of those, entries nothing claims today - the real gain.</param>
/// <param name="AlreadyOwned">
/// Of those, entries the rule being edited already claims. Only meaningful when editing:
/// these are kept, not gained, so an unchanged rule reports its whole reach here.
/// </param>
/// <param name="TakenFromOtherServices">
/// Of those, entries a different rule claims today. Non-zero means this rule is more specific
/// than an existing one and would take its entries, changing that service's statistics.
/// </param>
public sealed record RulePreview(
    int WouldMatchTotal,
    int CurrentlyUnmatched,
    int AlreadyOwned,
    int TakenFromOtherServices,
    IReadOnlyList<string> SampleNames,
    IReadOnlyList<string> AffectedServices);

public sealed class CatalogConflictException(string message) : Exception(message);

public sealed class CatalogNotFoundException(string message) : Exception(message);

/// <summary>
/// Administration of the canonical service catalog: listing the rules with their reach,
/// editing them, and - the part that makes editing practical - showing what a rule would
/// do to the corpus before it is saved, plus which names nothing currently claims.
/// </summary>
public sealed class CatalogService(
    ICanonicalServiceRepository catalogRepository,
    IMarketEntryRepository marketEntryRepository)
{
    private const int SampleNameLimit = 10;

    public async Task<IReadOnlyList<CatalogEntryView>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);
        var corpus = await marketEntryRepository.GetAllAsync(cancellationToken);

        var counts = CountBySlug(corpus, catalog);

        return catalog
            .Select(service => new CatalogEntryView(
                service.Id,
                service.Slug,
                service.DisplayName,
                service.RequiredPhrase,
                service.ExcludedWords,
                counts.GetValueOrDefault(service.Slug)))
            .OrderByDescending(view => view.MatchedEntries)
            .ToList();
    }

    public async Task<CatalogEntryView> CreateAsync(
        string slug,
        string displayName,
        string requiredPhrase,
        string? excludedWords,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (await catalogRepository.SlugExistsAsync(normalizedSlug, cancellationToken))
        {
            throw new CatalogConflictException($"Usluga s oznakom '{normalizedSlug}' već postoji.");
        }

        var service = new CanonicalService(normalizedSlug, displayName, requiredPhrase, excludedWords);
        await catalogRepository.AddAsync(service, cancellationToken);
        await catalogRepository.SaveChangesAsync(cancellationToken);

        return new CatalogEntryView(
            service.Id, service.Slug, service.DisplayName, service.RequiredPhrase, service.ExcludedWords, 0);
    }

    public async Task UpdateAsync(
        int id,
        string displayName,
        string requiredPhrase,
        string? excludedWords,
        CancellationToken cancellationToken = default)
    {
        var service = await catalogRepository.FindAsync(id, cancellationToken)
            ?? throw new CatalogNotFoundException("Usluga nije pronađena.");

        service.Update(displayName, requiredPhrase, excludedWords);
        await catalogRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await catalogRepository.FindAsync(id, cancellationToken)
            ?? throw new CatalogNotFoundException("Usluga nije pronađena.");

        await catalogRepository.RemoveAsync(service, cancellationToken);
        await catalogRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Traces how a single name is matched against the live catalog.</summary>
    public async Task<MatchExplanation> ExplainAsync(string rawName, CancellationToken cancellationToken = default)
    {
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);
        return ServiceMatcher.Explain(rawName, catalog);
    }

    /// <summary>
    /// Corpus names no rule currently claims, most frequent first - the working list for
    /// deciding which rule to add next.
    /// </summary>
    public async Task<IReadOnlyList<UnmatchedName>> GetUnmatchedNamesAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);
        var corpus = await marketEntryRepository.GetAllAsync(cancellationToken);

        return corpus
            .Where(entry => ServiceMatcher.Match(entry.Name, catalog) is null)
            .GroupBy(entry => entry.Name)
            .Select(group => new UnmatchedName(group.Key, group.Count()))
            .OrderByDescending(name => name.Occurrences)
            .ThenBy(name => name.Name)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Evaluates a rule against the corpus without saving it. When <paramref name="editingId"/>
    /// is given, that rule is replaced by the proposed one, so editing an existing rule is
    /// previewed against the catalog as it would be, not as it is.
    /// </summary>
    public async Task<RulePreview> PreviewAsync(
        string requiredPhrase,
        string? excludedWords,
        int? editingId = null,
        CancellationToken cancellationToken = default)
    {
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);
        var corpus = await marketEntryRepository.GetAllAsync(cancellationToken);

        const string previewSlug = "__preview__";
        var proposed = new CanonicalService(previewSlug, "Nova usluga", requiredPhrase, excludedWords);

        var withoutEdited = catalog.Where(service => service.Id != editingId).ToList();
        var proposedCatalog = withoutEdited.Append(proposed).ToList();

        var wouldMatch = new List<MarketEntry>();
        var currentlyUnmatched = 0;
        var alreadyOwned = 0;
        var takenFrom = new List<string>();

        foreach (var entry in corpus)
        {
            if (ServiceMatcher.Match(entry.Name, proposedCatalog)?.Slug != previewSlug)
            {
                continue;
            }

            wouldMatch.Add(entry);

            // Compare against the catalog as it stands today, including the rule being
            // edited - that is what the administrator sees on screen right now.
            var currentOwner = ServiceMatcher.Match(entry.Name, catalog);

            if (currentOwner is null)
            {
                currentlyUnmatched++;
            }
            else if (currentOwner.Id == editingId)
            {
                // The rule being edited already has this entry: kept, not taken.
                alreadyOwned++;
            }
            else
            {
                takenFrom.Add(currentOwner.DisplayName);
            }
        }

        return new RulePreview(
            wouldMatch.Count,
            currentlyUnmatched,
            alreadyOwned,
            takenFrom.Count,
            wouldMatch.Select(entry => entry.Name).Distinct().Take(SampleNameLimit).ToList(),
            takenFrom.Distinct().OrderBy(name => name).ToList());
    }

    private static Dictionary<string, int> CountBySlug(
        IReadOnlyList<MarketEntry> corpus,
        IReadOnlyList<CanonicalService> catalog)
    {
        var counts = new Dictionary<string, int>();

        foreach (var entry in corpus)
        {
            var match = ServiceMatcher.Match(entry.Name, catalog);
            if (match is not null)
            {
                counts[match.Slug] = counts.GetValueOrDefault(match.Slug) + 1;
            }
        }

        return counts;
    }
}
