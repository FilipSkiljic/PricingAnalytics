using System.Globalization;
using System.Text;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Csv;

/// <summary>
/// Reads the "Name","Price" market export into <see cref="MarketEntry"/> records.
/// Hand-rolled RFC4180-style parser (not a naive comma split) because the real
/// export contains embedded newlines and escaped quotes inside quoted fields,
/// e.g. a name spanning two lines or containing "" inside it. Lives in
/// Infrastructure because it is file I/O, not a domain rule.
/// </summary>
public static class CsvMarketEntryReader
{
    public static IReadOnlyList<MarketEntry> ReadFile(string path) => ReadText(File.ReadAllText(path));

    public static IReadOnlyList<MarketEntry> ReadText(string csvText)
    {
        return ReadNamePricePairs(csvText)
            .Select(pair => new MarketEntry(pair.Name, pair.Price))
            .ToList();
    }

    /// <summary>
    /// Parses the CSV into raw name/price pairs, skipping the header and any row with a
    /// blank name or unparseable price. Shared by the market corpus reader and by the
    /// uploaded price-list parser, which build different entities from the same shape.
    /// </summary>
    public static IReadOnlyList<(string Name, decimal Price)> ReadNamePricePairs(string csvText)
    {
        var records = ParseRecords(csvText);
        var pairs = new List<(string, decimal)>(records.Count);

        foreach (var record in records.Skip(1)) // skip header row
        {
            if (record.Count < 2)
            {
                continue;
            }

            var name = record[0].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            if (!decimal.TryParse(record[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            {
                continue;
            }

            pairs.Add((name, price));
        }

        return pairs;
    }

    private static List<List<string>> ParseRecords(string text)
    {
        var records = new List<List<string>>();
        var currentRecord = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                        continue;
                    }

                    inQuotes = false;
                    i++;
                    continue;
                }

                field.Append(c);
                i++;
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    i++;
                    break;
                case ',':
                    currentRecord.Add(field.ToString());
                    field.Clear();
                    i++;
                    break;
                case '\r':
                    i++;
                    break;
                case '\n':
                    currentRecord.Add(field.ToString());
                    field.Clear();
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                    i++;
                    break;
                default:
                    field.Append(c);
                    i++;
                    break;
            }
        }

        if (field.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(field.ToString());
            records.Add(currentRecord);
        }

        return records;
    }
}
