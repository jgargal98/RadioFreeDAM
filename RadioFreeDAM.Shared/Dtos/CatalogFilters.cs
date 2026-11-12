namespace RadioFreeDAM.Shared.Dtos;

/// <summary>One selectable value of a filter (an id the API understands and a label for the user).</summary>
public record FilterOption(string Id, string Name);

/// <summary>
/// Filters of the station catalog. The ids are the contract between the app and the API:
/// the app sends them as query parameters and the API validates them against these lists.
/// </summary>
public static class CatalogFilters
{
    public const string ScopeSpain = "spain";
    public const string ScopeWorld = "world";

    public const string AllCategories = "all";

    public const string SortClicks = "clicks";

    public static readonly IReadOnlyList<FilterOption> Scopes =
    [
        new(ScopeSpain, "España"),
        new(ScopeWorld, "Mundo")
    ];

    public static readonly IReadOnlyList<FilterOption> Categories =
    [
        new(AllCategories, "Todas"),
        new("news", "Noticias"),
        new("music", "Música"),
        new("sport", "Deporte"),
        new("pop", "Pop"),
        new("rock", "Rock"),
        new("dance", "Dance"),
        new("classical", "Clásica"),
        new("jazz", "Jazz"),
        new("latin", "Latina"),
        new("culture", "Cultura")
    ];

    public static readonly IReadOnlyList<FilterOption> Sorts =
    [
        new(SortClicks, "Más escuchadas"),
        new("trend", "Tendencia ahora"),
        new("votes", "Mejor valoradas"),
        new("name", "Nombre, de la A a la Z"),
        new("bitrate", "Mejor calidad de audio")
    ];

    public static bool IsValidScope(string id) => Scopes.Any(o => o.Id == id);

    public static bool IsValidCategory(string id) => Categories.Any(o => o.Id == id);

    public static bool IsValidSort(string id) => Sorts.Any(o => o.Id == id);
}
