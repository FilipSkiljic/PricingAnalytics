using PricingAnalytics.Domain.Normalization;

namespace PricingAnalytics.Domain.Matching;

/// <summary>
/// A single "real" service that many differently-typed market entry names should
/// collapse into (e.g. "Muško šišanje", "MUŠKO ŠIŠANJE", "Šišanje muško").
/// </summary>
/// <remarks>
/// Stores the human-written phrases rather than the derived tokens: an administrator
/// types "muško šišanje" and "pranje, feniranje", and the matching tokens are derived
/// from those by the same normalizer that processes market entry names. Keeping the
/// phrases means the rule stays readable and editable after it is saved.
/// </remarks>
public sealed class CanonicalService
{
    private HashSet<string>? _requiredTokens;
    private HashSet<string>? _excludedTokens;

    public int Id { get; private set; }

    /// <summary>Stable textual identifier, e.g. "musko-sisanje".</summary>
    public string Slug { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Phrase whose every word must appear in a name for it to match, e.g. "muško šišanje".</summary>
    public string RequiredPhrase { get; private set; } = string.Empty;

    /// <summary>
    /// Comma-separated words that disqualify a name, e.g. "pranje, feniranje". Their presence
    /// marks the entry as a different, bundled service whose price is not comparable.
    /// </summary>
    public string ExcludedWords { get; private set; } = string.Empty;

    /// <summary>Stemmed tokens that must ALL be present for a name to match.</summary>
    public IReadOnlySet<string> RequiredTokens => _requiredTokens ??= TextNormalizer.Tokenize(RequiredPhrase).ToHashSet();

    /// <summary>
    /// Stemmed tokens whose presence disqualifies a name. Words that also appear in the
    /// required phrase are dropped, since a rule cannot both require and forbid a word.
    /// </summary>
    public IReadOnlySet<string> ExcludedTokens => _excludedTokens ??= SplitExcluded()
        .SelectMany(TextNormalizer.Tokenize)
        .Where(token => !RequiredTokens.Contains(token))
        .ToHashSet();

    private CanonicalService()
    {
        // EF Core materialization.
    }

    public CanonicalService(string slug, string displayName, string requiredPhrase, string? excludedWords = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredPhrase);

        Slug = slug.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        RequiredPhrase = requiredPhrase.Trim();
        ExcludedWords = (excludedWords ?? string.Empty).Trim();

        EnsureRequiredPhraseProducesTokens();
    }

    public void Update(string displayName, string requiredPhrase, string? excludedWords)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredPhrase);

        DisplayName = displayName.Trim();
        RequiredPhrase = requiredPhrase.Trim();
        ExcludedWords = (excludedWords ?? string.Empty).Trim();

        // Phrases changed, so the cached tokens no longer describe this rule.
        _requiredTokens = null;
        _excludedTokens = null;

        EnsureRequiredPhraseProducesTokens();
    }

    private IEnumerable<string> SplitExcluded() =>
        ExcludedWords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// A phrase made only of punctuation would normalize to nothing and then match every
    /// name in the corpus, so it is rejected at the source.
    /// </summary>
    private void EnsureRequiredPhraseProducesTokens()
    {
        if (RequiredTokens.Count == 0)
        {
            throw new ArgumentException(
                $"Tražene riječi '{RequiredPhrase}' ne daju nijednu upotrebljivu riječ.", nameof(RequiredPhrase));
        }
    }
}
