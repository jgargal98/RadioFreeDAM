using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>What the app asks the catalog for. Ids come from <see cref="CatalogFilters"/>.</summary>
public record CatalogRequest(string? Name, string Scope, string Category, string Sort)
{
    public static readonly CatalogRequest Default =
        new(null, CatalogFilters.ScopeSpain, CatalogFilters.AllCategories, CatalogFilters.SortClicks);
}

/// <summary>
/// Catalog as the app shows it: filtered by scope, category and sort order, and with the stations of the
/// same network (COPE Málaga, COPE Sevilla...) joined in one row from which the city is chosen.
/// </summary>
public class CatalogService(IStationService stations)
{
    /// <summary>Rows shown in the home list when there is no text search.</summary>
    public const int TopEntries = 20;

    /// <summary>
    /// More stations than rows are requested because grouping joins several of them in one row.
    /// A text search shows every match, so it needs fewer.
    /// </summary>
    private const int TopFetchLimit = 80;
    private const int SearchFetchLimit = 60;
    private const int NetworkFetchLimit = 100;

    /// <exception cref="ValidationFailedException">If a filter id is unknown.</exception>
    public async Task<IReadOnlyList<CatalogEntryDto>> GetAsync(CatalogRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var isSearch = !string.IsNullOrWhiteSpace(request.Name);
        var query = new StationQuery(
            request.Name?.Trim(),
            CountryOnly: request.Scope == CatalogFilters.ScopeSpain,
            Tags: CategoryTags.For(request.Category),
            Sort: request.Sort,
            Limit: isSearch ? SearchFetchLimit : TopFetchLimit);

        var found = await stations.QueryAsync(query, cancellationToken);
        return Group(found, isSearch ? int.MaxValue : TopEntries);
    }

    /// <summary>All the stations of a network in the configured country, from the most to the least listened.</summary>
    /// <exception cref="NotFoundAppException">If the network does not exist.</exception>
    public async Task<IReadOnlyList<StationDto>> GetNetworkStationsAsync(string networkId, CancellationToken cancellationToken = default)
    {
        var network = NetworkCatalog.FindById(networkId)
            ?? throw new NotFoundAppException("Esa cadena no existe.");

        // One search per way of writing the name ("esradio" and "es radio"); the results are merged afterwards.
        var found = new List<StationDto>();
        foreach (var term in network.SearchTerms)
        {
            var query = new StationQuery(term, CountryOnly: true, Tags: [], CatalogFilters.SortClicks, NetworkFetchLimit);
            found.AddRange(await stations.QueryAsync(query, cancellationToken));
        }

        return found
            .Where(s => network.Contains(s.Name))
            .DistinctBy(s => s.Id)
            .OrderByDescending(s => s.ClickCount)
            .ToList();
    }

    private static void Validate(CatalogRequest request)
    {
        if (!CatalogFilters.IsValidScope(request.Scope))
            throw new ValidationFailedException("El ámbito no es válido.");
        if (!CatalogFilters.IsValidCategory(request.Category))
            throw new ValidationFailedException("La categoría no es válida.");
        if (!CatalogFilters.IsValidSort(request.Sort))
            throw new ValidationFailedException("El orden no es válido.");
    }

    /// <summary>
    /// Joins in one row the stations of the same network. A network is only grouped if it appears more than once:
    /// a single COPE in the list is shown as a plain station. Order is respected (the network row goes where
    /// its most popular station was).
    /// </summary>
    public static IReadOnlyList<CatalogEntryDto> Group(IReadOnlyList<StationDto> stations, int limit)
    {
        string? NetworkOf(StationDto station) => station.NetworkId ?? NetworkCatalog.FindFor(station.Name)?.Id;

        var byNetwork = stations
            .Where(s => NetworkOf(s) is not null)
            .GroupBy(s => NetworkOf(s)!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var entries = new List<CatalogEntryDto>();
        var added = new HashSet<string>();

        foreach (var station in stations)
        {
            if (entries.Count >= limit)
                break;

            var networkId = NetworkOf(station);
            if (networkId is null || byNetwork[networkId].Count < 2)
            {
                entries.Add(new CatalogEntryDto { Station = station });
                continue;
            }

            if (!added.Add(networkId))
                continue; // the row of this network is already there

            entries.Add(new CatalogEntryDto
            {
                Network = new NetworkDto
                {
                    Id = networkId,
                    Name = NetworkCatalog.FindById(networkId)!.Name,
                    FaviconUrl = byNetwork[networkId].FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.FaviconUrl))?.FaviconUrl
                }
            });
        }

        return entries;
    }
}
