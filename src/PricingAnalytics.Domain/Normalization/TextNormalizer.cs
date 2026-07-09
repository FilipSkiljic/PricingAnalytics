using System.Text;

namespace PricingAnalytics.Domain.Normalization;

/// <summary>
/// Normalizes raw, hand-typed service names from POS exports into a comparable form.
/// Real data has diacritics typed inconsistently (š/s), mixed case, stray punctuation
/// and duplicate whitespace - this class removes all of that noise before matching.
/// </summary>
public static class TextNormalizer
{
    private static readonly Dictionary<char, char> DiacriticsMap = new()
    {
        ['š'] = 's',
        ['đ'] = 'd',
        ['č'] = 'c',
        ['ć'] = 'c',
        ['ž'] = 'z',
    };

    // Suffixes are checked longest-first so e.g. "ama" is stripped before "a".
    // Only applied when the remaining stem is still long enough to be meaningful -
    // this is a small rule-based stemmer for Croatian nouns/adjectives, not a full
    // morphological analyzer. It exists purely to unify case-inflected variants of
    // the same word (e.g. "mašinicom" / "mašinicu" / "mašinica").
    private static readonly string[] InflectionSuffixes =
    {
        "ama", "ima", "ovima", "evima",
        "om", "em", "og", "eg", "oj", "ih", "ju",
        "a", "e", "i", "o", "u",
    };

    private const int MinStemLength = 4;

    /// <summary>Trims and collapses internal whitespace, preserving original casing/diacritics for display.</summary>
    public static string NormalizeForDisplay(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return CollapseWhitespace(raw.Trim());
    }

    /// <summary>
    /// Produces a lowercase, diacritic-free, punctuation-free key suitable for exact-match
    /// grouping. Two names that only differ in case, diacritics, punctuation or spacing
    /// resolve to the same key.
    /// </summary>
    public static string NormalizeKey(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim().ToLowerInvariant())
        {
            if (DiacriticsMap.TryGetValue(ch, out var replacement))
            {
                sb.Append(replacement);
            }
            else if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else
            {
                sb.Append(' '); // punctuation/symbols become a token boundary
            }
        }

        return CollapseWhitespace(sb.ToString().Trim());
    }

    /// <summary>Splits a raw name into normalized, stemmed tokens (order preserved, duplicates kept).</summary>
    public static IReadOnlyList<string> Tokenize(string raw)
    {
        var key = NormalizeKey(raw);
        if (key.Length == 0)
        {
            return Array.Empty<string>();
        }

        return key.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(Stem)
            .ToArray();
    }

    /// <summary>Strips a common Croatian inflectional suffix from a single normalized token.</summary>
    public static string Stem(string token)
    {
        foreach (var suffix in InflectionSuffixes)
        {
            if (token.Length - suffix.Length >= MinStemLength && token.EndsWith(suffix, StringComparison.Ordinal))
            {
                return token[..^suffix.Length];
            }
        }

        return token;
    }

    private static string CollapseWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        var lastWasSpace = false;
        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                }

                lastWasSpace = true;
            }
            else
            {
                sb.Append(ch);
                lastWasSpace = false;
            }
        }

        if (sb.Length > 0 && sb[^1] == ' ')
        {
            sb.Length--;
        }

        return sb.ToString();
    }
}
