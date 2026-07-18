using PricingAnalytics.Domain.Normalization;

namespace PricingAnalytics.Domain.Matching;

public static class ServiceMatcher
{
    /// <summary>
    /// Matches a raw, hand-typed market entry name to a canonical service.
    /// A candidate matches when all of its required tokens are present in the name
    /// and none of its excluded (bundle-indicator) tokens are present. When several
    /// candidates match (e.g. a generic and a more specific service), the one with
    /// the most required tokens wins, since it is the more specific description.
    /// </summary>
    public static CanonicalService? Match(string rawName, IEnumerable<CanonicalService> catalog)
    {
        ArgumentNullException.ThrowIfNull(rawName);
        ArgumentNullException.ThrowIfNull(catalog);

        var tokens = TextNormalizer.Tokenize(rawName).ToHashSet();
        if (tokens.Count == 0)
        {
            return null;
        }

        return catalog
            .Where(c => c.RequiredTokens.IsSubsetOf(tokens) && !c.ExcludedTokens.Overlaps(tokens))
            .OrderByDescending(c => c.RequiredTokens.Count)
            .FirstOrDefault();
    }

    /// <summary>
    /// Same decision as <see cref="Match"/>, but reports how it was reached: which words
    /// the name reduced to, which rules qualified, and for each rule that did not, whether
    /// it was missing a required word or blocked by an excluded one.
    /// </summary>
    public static MatchExplanation Explain(string rawName, IEnumerable<CanonicalService> catalog)
    {
        ArgumentNullException.ThrowIfNull(rawName);
        ArgumentNullException.ThrowIfNull(catalog);

        var tokens = TextNormalizer.Tokenize(rawName).ToHashSet();

        var candidates = catalog
            .Select(service =>
            {
                var missing = service.RequiredTokens.Where(t => !tokens.Contains(t)).ToList();
                var blocking = service.ExcludedTokens.Where(tokens.Contains).ToList();

                return new CandidateExplanation(
                    service.Slug,
                    service.DisplayName,
                    missing,
                    blocking,
                    IsCandidate: tokens.Count > 0 && missing.Count == 0 && blocking.Count == 0);
            })
            .ToList();

        // Same tie-break as Match: most required words wins, so the explanation can never
        // disagree with the decision the analysis actually uses.
        var winnerSlug = Match(rawName, catalog)?.Slug;
        var winner = candidates.FirstOrDefault(c => c.Slug == winnerSlug);

        return new MatchExplanation(rawName, tokens.ToList(), winner, candidates);
    }
}
