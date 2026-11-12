using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// What the catalog is asked for: an optional name, the country scope, the tags of a category and the sort order.
/// </summary>
/// <param name="Tags">Radio Browser tags to look for. Empty means no category filter.</param>
public record StationQuery(string? Name, bool CountryOnly, IReadOnlyList<string> Tags, string Sort, int Limit);

/// <summary>Sorting rules shared by the real catalog and by the test doubles.</summary>
public static class StationSort
{
    /// <summary>Value of Radio Browser's "order" parameter and whether it is descending.</summary>
    public static (string Order, bool Reverse) ToRadioBrowser(string sort) => sort switch
    {
        "trend" => ("clicktrend", true),
        "votes" => ("votes", true),
        // Alphabetical order of the whole catalog starts with junk names; instead the most listened are fetched and sorted here.
        "name" => ("clickcount", true),
        "bitrate" => ("bitrate", true),
        _ => ("clickcount", true)
    };

    /// <summary>
    /// Sorts in memory. Needed because a category can mean several Radio Browser tags: each tag is a
    /// separate request, and their results have to be merged and ordered again.
    /// </summary>
    public static IReadOnlyList<StationDto> Apply(IEnumerable<StationDto> stations, string sort) => sort switch
    {
        "trend" => stations.OrderByDescending(s => s.ClickTrend).ToList(),
        "votes" => stations.OrderByDescending(s => s.Votes).ToList(),
        // Leading symbols ("!MODA", "__80 EXITOS") are ignored so the list starts at the first real letter.
        "name" => stations.OrderBy(s => string.Concat(s.Name.SkipWhile(c => !char.IsLetterOrDigit(c))), StringComparer.CurrentCultureIgnoreCase).ToList(),
        "bitrate" => stations.OrderByDescending(s => s.Bitrate).ToList(),
        _ => stations.OrderByDescending(s => s.ClickCount).ToList()
    };
}

/// <summary>Radio Browser tags behind each category of the filter.</summary>
public static class CategoryTags
{
    private static readonly Dictionary<string, string[]> Map = new()
    {
        ["news"] = ["news", "noticias"],
        ["music"] = ["music", "musica"],
        ["sport"] = ["sport", "deportes"],
        ["pop"] = ["pop"],
        ["rock"] = ["rock"],
        ["dance"] = ["dance"],
        ["classical"] = ["classical", "clasica"],
        ["jazz"] = ["jazz"],
        ["latin"] = ["latin", "latina"],
        ["culture"] = ["culture", "cultura"]
    };

    /// <summary>Tags of a category; empty for "all" or an unknown id.</summary>
    public static IReadOnlyList<string> For(string category) =>
        Map.TryGetValue(category, out var tags) ? tags : [];
}
