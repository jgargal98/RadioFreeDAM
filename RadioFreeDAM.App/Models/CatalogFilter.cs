namespace RadioFreeDAM.Models;

/// <summary>What the user is looking at in the home list. Ids come from <see cref="CatalogFilters"/>.</summary>
/// <param name="Name">Text typed in the search box, or null when there is no search.</param>
public record CatalogFilter(string? Name, string Scope, string Category, string Sort)
{
    public static readonly CatalogFilter Default =
        new(null, CatalogFilters.ScopeSpain, CatalogFilters.AllCategories, CatalogFilters.SortClicks);

    /// <summary>True when filters (not the search text) differ from the defaults.</summary>
    public bool HasActiveFilters => this with { Name = null } != Default;
}
