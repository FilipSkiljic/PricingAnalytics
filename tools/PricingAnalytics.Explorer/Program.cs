using PricingAnalytics.Domain.Matching;
using PricingAnalytics.Domain.Statistics;
using PricingAnalytics.Infrastructure.Csv;

var csvPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "prices.csv");
csvPath = Path.GetFullPath(csvPath);

Console.WriteLine($"Loading: {csvPath}");
var entries = CsvMarketEntryReader.ReadFile(csvPath);
Console.WriteLine($"Loaded {entries.Count} market entries.");

// Uses the starting catalog rather than whatever is in the database, so the tool runs
// standalone against a CSV without needing a database connection. Rules added through
// the admin screen therefore will not show up here.
var catalog = CanonicalServiceCatalog.SeedData();
var grouped = entries
    .Select(e => (Entry: e, Match: ServiceMatcher.Match(e.Name, catalog)))
    .GroupBy(x => x.Match?.Slug ?? "(unmatched)")
    .OrderByDescending(g => g.Count())
    .ToList();

Console.WriteLine();
Console.WriteLine("Canonical service breakdown:");
foreach (var group in grouped)
{
    var displayName = group.First().Match?.DisplayName ?? "(unmatched)";
    var prices = group.Select(x => x.Entry.Price).ToList();

    if (group.Key == "(unmatched)")
    {
        Console.WriteLine($"  {displayName,-24} n={group.Count(),-5}");
        continue;
    }

    var clean = OutlierFilter.RemoveIqrOutliers(prices);
    var (q1, median, q3) = DescriptiveStatistics.Quartiles(clean.Count > 0 ? clean : prices);
    Console.WriteLine($"  {displayName,-24} n={group.Count(),-5} (clean n={clean.Count,-5}) median={median,7:0.00} Q1={q1,7:0.00} Q3={q3,7:0.00} min={prices.Min(),7:0.00} max={prices.Max(),7:0.00}");
}

Console.WriteLine();
Console.WriteLine("Top 15 unmatched names (by frequency), to guide catalog growth:");
var unmatched = grouped.FirstOrDefault(g => g.Key == "(unmatched)");
if (unmatched is not null)
{
    foreach (var name in unmatched.Select(x => x.Entry.Name).GroupBy(n => n).OrderByDescending(g => g.Count()).Take(15))
    {
        Console.WriteLine($"  {name.Count(),4}  {name.Key}");
    }
}
