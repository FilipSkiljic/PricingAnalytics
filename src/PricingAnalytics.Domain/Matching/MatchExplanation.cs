namespace PricingAnalytics.Domain.Matching;

/// <summary>Why one canonical service did or did not claim a given name.</summary>
/// <param name="MissingTokens">Required words the name lacks. Empty when the rule's words are all present.</param>
/// <param name="BlockingTokens">Excluded words found in the name. Empty when nothing disqualifies it.</param>
/// <param name="IsCandidate">True when the rule qualifies - all required words present, none excluded.</param>
public sealed record CandidateExplanation(
    string Slug,
    string DisplayName,
    IReadOnlyList<string> MissingTokens,
    IReadOnlyList<string> BlockingTokens,
    bool IsCandidate);

/// <summary>
/// Full trace of a match attempt: what the name normalized to, which rules qualified,
/// which one won, and why each of the others did not. Drives the catalog editor's
/// "why did this not match?" view.
/// </summary>
/// <param name="Winner">
/// The chosen service, or null when nothing qualified. When several rules qualify the
/// most specific one wins - the one with the most required words.
/// </param>
public sealed record MatchExplanation(
    string RawName,
    IReadOnlyList<string> NormalizedTokens,
    CandidateExplanation? Winner,
    IReadOnlyList<CandidateExplanation> AllCandidates);
